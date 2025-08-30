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
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.XamlUtilities;
using CefSharp;
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
            new SettingsFeature(this)
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

        foreach (var feature in _features)
        {
            if (feature.HandleOnPreviewKeyDown(e))
            {
                e.Handled = true;
                return;
            }
        }

        // Too small to be handled by features, handle here
        if (e.Key == Key.F5)
        {
            var ignoreCache = Keyboard.Modifiers == ModifierKeys.Control;
            CurrentTab.Reload(ignoreCache);
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

    private static void OnWorkspaceColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var window = (MainWindow)d;
        var newColor = (Color)e.NewValue;
        var border = window.WindowBorder;
        if (border.Background is SolidColorBrush brush)
        {
            var animation = new ColorAnimation
            {
                To = newColor,
                Duration = TimeSpan.FromSeconds(1),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
        }
        else
        {
            border.Background = new SolidColorBrush(newColor);
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
            _tabPaletteHasBeenShown = true;
        }

        TabPaletteBrowserControl.Visibility = Visibility.Visible;
        TabPaletteGridSplitter.Visibility = Visibility.Visible;
        // Set the desired expanded width for the column before expanding
        var duration = TimeSpan.FromMilliseconds(300);
        GridAnimationBehavior.SetDuration(TabPaletteColumn, duration);
        GridAnimationBehavior.SetIsExpanded(TabPaletteColumn, true);
    }

    public void HideTabPalette()
    {
        if (TabPaletteBrowserControl.Visibility != Visibility.Visible)
            return;

        var duration = TimeSpan.FromMilliseconds(200);
        GridAnimationBehavior.SetDuration(TabPaletteColumn, duration);
        GridAnimationBehavior.SetIsExpanded(TabPaletteColumn, false);
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
