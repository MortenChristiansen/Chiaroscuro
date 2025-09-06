using BrowserHost.Features;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.Folders;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DevTool;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.XamlUtilities;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BrowserHost;

public partial class MainWindow : Window
{
    private readonly List<Feature> _features;
    private bool _tabPaletteHasBeenShown;
    private const double _lightenFactor = 0.04;

    public ChromiumWebBrowser Chrome => ChromeUI;
    public TabBrowser? CurrentTab => (TabBrowser)WebContentBorder.Child;

    public static MainWindow Instance { get; private set; } = null!; // Initialized in constructor

    public static readonly DependencyProperty WorkspaceColorProperty = DependencyProperty.Register(
        nameof(WorkspaceColor), typeof(Color), typeof(MainWindow),
        new PropertyMetadata(Color.FromArgb(0, 0, 0, 0), OnWorkspaceColorChanged));

    public Color WorkspaceColor
    {
        get => (Color)GetValue(WorkspaceColorProperty);
        set => SetValue(WorkspaceColorProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();

        CheckForUpdates();

        _features =
        [
            new SettingsFeature(this),
            new CustomWindowChromeFeature(this),
            new ActionDialogFeature(this),
            new TabsFeature(this),
            new PinnedTabsFeature(this),
            new DevToolFeature(this),
            new FileDownloadsFeature(this),
            new ZoomFeature(this),
            new DragDropFeature(this),
            new WorkspacesFeature(this),
            new FoldersFeature(this),
            new TabPaletteFeature(this),
            new FindTextFeature(this),
            new TabCustomizationFeature(this),
            new DomainCustomizationFeature(this),
        ];
        _features.ForEach(f => f.Configure());

        ContentServer.Run();
        Instance = this;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        _features.ForEach(f => f.Start());
        base.OnContentRendered(e);
    }

    public TFeature GetFeature<TFeature>() where TFeature : Feature
    {
        return _features.OfType<TFeature>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Feature of type {typeof(TFeature).Name} not found.");
    }

    private static async void CheckForUpdates()
    {
        try
        {
            if (!App.UpdateManager.IsInstalled)
                return;

            var updateInfo = await App.UpdateManager.CheckForUpdatesAsync();
            if (updateInfo != null)
            {
                var result = MessageBox.Show(
                    Application.Current.MainWindow,
                    "A new version is available. Would you like to update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    await App.UpdateManager.DownloadUpdatesAsync(updateInfo);
                    App.UpdateManager.ApplyUpdatesAndRestart(updateInfo);
                }
            }
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        ProcessKeyboardEvent(e);
    }

    public void ProcessKeyboardEvent(KeyEventArgs e)
    {
        if (e.Handled) return; // Already handled
        foreach (var feature in _features)
        {
            if (feature.HandleOnPreviewKeyDown(e))
            {
                e.Handled = true;
                return;
            }
        }
        if (!e.Handled && e.Key == Key.F5)
        {
            var ignoreCache = Keyboard.Modifiers == ModifierKeys.Control;
            CurrentTab?.Reload(ignoreCache);
            e.Handled = true;
        }
    }

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnPreviewMouseWheel(e);

        foreach (var feature in _features)
        {
            if (feature.HandleOnPreviewMouseWheel(e))
            {
                e.Handled = true;
                return;
            }
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var downloadsFeature = GetFeature<FileDownloadsFeature>();
        if (downloadsFeature.HasActiveDownloads())
        {
            var result = MessageBox.Show(
                this,
                "There are active downloads. Are you sure you want to exit? All downloads will be cancelled.",
                "Active Downloads",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            downloadsFeature.CancelAllActiveDownloads();
        }
        base.OnClosing(e);
    }

    public void SetCurrentTab(TabBrowser? tab)
    {
        if (CurrentTab != null)
            CurrentTab.AddressChanged -= Tab_AddressChanged;

        WebContentBorder.Child = tab;
        ChromeUI.ChangeAddress(GetAddressForPresentation(tab?.Address));

        if (tab != null)
            tab.AddressChanged += Tab_AddressChanged;
    }

    private void Tab_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ChromeUI.ChangeAddress(GetAddressForPresentation($"{e.NewValue}"));
    }

    private static string? GetAddressForPresentation(string? address)
    {
        if (address != null && ContentServer.IsContentPage(address, out var contentPage, ContentPageUrlMode.Absolute))
            return contentPage.Address;

        return address;
    }

    private static Color Lighten(Color color, double factor)
    {
        byte L(byte c) => (byte)Math.Clamp(c + (255 - c) * factor, 0, 255);
        return Color.FromArgb(color.A, L(color.R), L(color.G), L(color.B));
    }

    private static void OnWorkspaceColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var window = (MainWindow)d;

        var newColor = (Color)e.NewValue;
        AnimateBackgroundColor(newColor, window.WindowBorder);

        var lightenedColor = Lighten(newColor, _lightenFactor);
        AnimateBackgroundColor(lightenedColor, window.WebContentBorder);
        AnimateBackgroundColor(lightenedColor, window.TabPaletteBorder);
    }

    private static void AnimateBackgroundColor(Color contentColor, System.Windows.Controls.Border container)
    {
        if (container.Background is SolidColorBrush contentBrush)
        {
            var animation = new ColorAnimation
            {
                To = contentColor,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            contentBrush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        else
        {
            container.Background = new SolidColorBrush(contentColor);
        }
    }

    public void ShowTabPalette()
    {
        if (TabPaletteBrowserControl.Visibility == Visibility.Visible)
            return;

        TabPaletteColumn.Width = new GridLength(350);

        if (!_tabPaletteHasBeenShown)
        {
            // We need to initialize the animation because the ActualWidth property does not have a value yet
            GridAnimationBehavior.Initialize(TabPaletteColumn);
            // Initialize the splitter column to its expanded width (5) so it can animate without popping
            TabPaletteSplitterColumn.Width = new GridLength(5);
            GridAnimationBehavior.Initialize(TabPaletteSplitterColumn);
            _tabPaletteHasBeenShown = true;
        }

        // Ensure tab palette background matches the current lightened workspace color before showing
        var lightenedColor = Lighten(WorkspaceColor, _lightenFactor);
        TabPaletteBorder.Background = new SolidColorBrush(lightenedColor);

        TabPaletteBrowserControl.Visibility = Visibility.Visible;
        TabPaletteGridSplitter.Visibility = Visibility.Visible;
        // Set the desired expanded width for the column before expanding
        var duration = TimeSpan.FromMilliseconds(300);
        GridAnimationBehavior.SetDuration(TabPaletteColumn, duration);
        GridAnimationBehavior.SetIsExpanded(TabPaletteColumn, true);
        // Animate the splitter column from 0 -> 5 smoothly
        GridAnimationBehavior.SetDuration(TabPaletteSplitterColumn, duration);
        GridAnimationBehavior.SetIsExpanded(TabPaletteSplitterColumn, true);
    }

    public void HideTabPalette()
    {
        if (TabPaletteBrowserControl.Visibility != Visibility.Visible)
            return;

        var duration = TimeSpan.FromMilliseconds(200);
        GridAnimationBehavior.SetDuration(TabPaletteColumn, duration);
        GridAnimationBehavior.SetIsExpanded(TabPaletteColumn, false);
        // Collapse the splitter column from 5 -> 0 smoothly
        GridAnimationBehavior.SetDuration(TabPaletteSplitterColumn, duration);
        GridAnimationBehavior.SetIsExpanded(TabPaletteSplitterColumn, false);
        // Hide controls after animation completes
        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            TabPaletteBrowserControl.Visibility = Visibility.Collapsed;
            TabPaletteGridSplitter.Visibility = Visibility.Collapsed;
        };
        timer.Start();
    }
}
