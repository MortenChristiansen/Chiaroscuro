using BrowserHost.Features;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.Folders;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.AppState;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DevTool;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.LocalWebApp;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.Terminal;
using BrowserHost.Features.Zoom;
using BrowserHost.Logging;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using BrowserHost.XamlUtilities;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Testably.Abstractions;
using Velopack;
using Measurement = BrowserHost.Logging.Measure;

namespace BrowserHost;

public partial class MainWindow : Window
{
    private readonly List<Feature> _features;
    private bool _tabPaletteHasBeenShown;
    private const int CornerRadiusDip = 8;
    private readonly AppStateStateManager _appStateStateManager;
    private readonly LocalWebAppProcessManager _localWebAppProcessManager;

    public CustomWindowChromeBrowser ChromeUI { get; }
    public ActionContextBrowser ActionContext { get; }
    public TabPaletteBrowser TabPaletteBrowserControl { get; }
    public ActionDialogBrowser ActionDialog { get; }
    public TerminalBrowser TerminalBrowserControl { get; }

    public ChromiumWebBrowser Chrome => ChromeUI;
    public TabBrowser? CurrentTab => (TabBrowser)WebContentBorder.Child;

    public ChromiumWebBrowser ActionDialogUi => ActionDialog;

    public static MainWindow Instance { get; private set; } = null!; // Initialized in constructor

    public static readonly DependencyProperty WorkspaceColorProperty = DependencyProperty.Register(
        nameof(WorkspaceColor), typeof(Color), typeof(MainWindow),
        new PropertyMetadata(Color.FromArgb(0, 0, 0, 0), OnWorkspaceColorChanged));

    public Color WorkspaceColor
    {
        get => (Color)GetValue(WorkspaceColorProperty);
        set => SetValue(WorkspaceColorProperty, value);
    }

    public CustomWindowChromeBrowserApi CustomWindowChromeBrowserApi { get; }
    public ActionDialogBrowserApi ActionDialogBrowserApi { get; }
    public TabsBrowserApi TabsBrowserApi { get; }
    public PinnedTabsBrowserApi PinnedTabsBrowserApi { get; }
    public DownloadsBrowserApi DownloadsBrowserApi { get; }
    public WorkspacesBrowserApi WorkspacesBrowserApi { get; }
    public FindTextBrowserApi FindTextBrowserApi { get; }
    public DomainCustomizationBrowserApi DomainCustomizationBrowserApi { get; }
    public TabPaletteBrowserApi TabPaletteBrowserApi { get; }
    public TabCustomizationBrowserApi TabCustomizationBrowserApi { get; }
    public LocalWebAppBrowserApi LocalWebAppBrowserApi { get; }
    public TerminalBrowserApi TerminalBrowserApi { get; }

    public MainWindow()
        : this(App.SettingsFeature, App.PubSub, App.FileSystem, browserContext: null, enableUpdateCheck: true, startContentServer: true)
    {
    }

    public MainWindow(SettingsFeature settingsFeature, PubSub pubSub, IFileSystem fileSystem, IBrowserContext? browserContext, bool enableUpdateCheck, bool startContentServer)
    {
        InitializeComponent();

        ChromeUI = new CustomWindowChromeBrowser(pubSub);
        ActionContext = new ActionContextBrowser(pubSub);
        TabPaletteBrowserControl = new TabPaletteBrowser(pubSub)
        {
            Visibility = Visibility.Collapsed
        };
        ActionDialog = new ActionDialogBrowser(pubSub)
        {
            Visibility = Visibility.Hidden
        };
        TerminalBrowserControl = new TerminalBrowser(pubSub)
        {
            Visibility = Visibility.Collapsed
        };

        ChromeUIHost.Content = ChromeUI;
        ActionContextHost.Content = ActionContext;
        TabPaletteBrowserHost.Content = TabPaletteBrowserControl;
        ActionDialogHost.Content = ActionDialog;
        TerminalBrowserHost.Content = TerminalBrowserControl;

        if (enableUpdateCheck)
        {
            // Defer update check until the window is fully initialized (owner is non-null).
            // Queued at ContextIdle to avoid competing with startup work.
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, CheckForUpdates);
        }

        CustomWindowChromeBrowserApi = new CustomWindowChromeBrowserApi(ChromeUI);
        ActionDialogBrowserApi = new ActionDialogBrowserApi(ActionDialog);
        TabsBrowserApi = new TabsBrowserApi(ActionContext);
        PinnedTabsBrowserApi = new PinnedTabsBrowserApi(ActionContext);
        DownloadsBrowserApi = new DownloadsBrowserApi(ActionContext);
        WorkspacesBrowserApi = new WorkspacesBrowserApi(ActionContext);
        FindTextBrowserApi = new FindTextBrowserApi(TabPaletteBrowserControl);
        DomainCustomizationBrowserApi = new DomainCustomizationBrowserApi(TabPaletteBrowserControl);
        TabPaletteBrowserApi = new TabPaletteBrowserApi(TabPaletteBrowserControl);
        TabCustomizationBrowserApi = new TabCustomizationBrowserApi(TabPaletteBrowserControl);
        LocalWebAppBrowserApi = new LocalWebAppBrowserApi(TabPaletteBrowserControl);
        TerminalBrowserApi = new TerminalBrowserApi(TerminalBrowserControl);

        browserContext ??= new BrowserContext(this);

        var timeSystem = new RealTimeSystem();
        var shellFileOpener = new ShellFileOpener();

        var customWindowChromeWindowOperations = new CustomWindowChromeWindowOperations(this);
        var actionDialogWindowOperations = new ActionDialogWindowOperations(this);
        var actionContextWindowOperations = new ActionContextWindowOperations(this);
        var tabPaletteWindowOperations = new TabPaletteWindowOperations(this);
        var devToolWindowOperations = new DevToolWindowOperations(this);
        var terminalWindowOperations = new TerminalWindowOperations(this, TerminalRow, TerminalSplitterRow, TerminalBrowserControl, TerminalGridSplitter);

        _appStateStateManager = new AppStateStateManager(fileSystem);
        _localWebAppProcessManager = new LocalWebAppProcessManager(pubSub);

        _features =
        [
            settingsFeature,
            new CustomWindowChromeFeature(pubSub, browserContext, CustomWindowChromeBrowserApi, customWindowChromeWindowOperations),
            new ActionContextFeature(pubSub, browserContext, actionContextWindowOperations),
            new ActionDialogFeature(pubSub, browserContext, ActionDialogBrowserApi, new NavigationHistoryStateManager(fileSystem), actionDialogWindowOperations),
            new TabsFeature(pubSub, browserContext, TabsBrowserApi),
            new PinnedTabsFeature(pubSub, browserContext, TabsBrowserApi, PinnedTabsBrowserApi, new PinnedTabsStateManager(fileSystem)),
            new DevToolFeature(pubSub, browserContext, devToolWindowOperations),
            new FileDownloadsFeature(pubSub, DownloadsBrowserApi, timeSystem),
            new ZoomFeature(pubSub, browserContext),
            new DragDropFeature(pubSub, browserContext, fileSystem),
            new WorkspacesFeature(pubSub, browserContext, WorkspacesBrowserApi, TabsBrowserApi, new WorkspaceStateManager(pubSub, fileSystem)),
            new FoldersFeature(pubSub, browserContext, TabsBrowserApi),
            new TabPaletteFeature(pubSub, browserContext, TabPaletteBrowserApi, tabPaletteWindowOperations),
            new FindTextFeature(pubSub, browserContext, FindTextBrowserApi, tabPaletteWindowOperations),
            new TabCustomizationFeature(pubSub, browserContext, TabCustomizationBrowserApi, TabsBrowserApi, new TabCustomizationStateManager(fileSystem)),
            new DomainCustomizationFeature(pubSub, browserContext, DomainCustomizationBrowserApi, new DomainCustomizationStateManager(fileSystem, shellFileOpener)),
            new LocalWebAppFeature(pubSub, browserContext, LocalWebAppBrowserApi, new LocalWebAppStateManager(fileSystem), _localWebAppProcessManager),
            new TerminalFeature(pubSub, browserContext, TerminalBrowserApi, terminalWindowOperations),
            new AppStateFeature(pubSub, actionContextWindowOperations, tabPaletteWindowOperations, _appStateStateManager),
        ];
        _features.ForEach(f =>
        {
            using (Measurement.Operation($"Configuring feature: {f.GetType().Name}"))
            {
                f.Configure();
            }
        });

        if (startContentServer)
        {
            using (Measurement.Operation("Starting content server"))
            {
                ContentServer.Run();
            }
        }

        Instance = this;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        _features.ForEach(f =>
        {
            using (Measurement.Operation($"Starting feature: {f.GetType().Name}"))
            {
                f.Start();
            }
        });
        base.OnContentRendered(e);
    }

    public TFeature GetFeature<TFeature>() where TFeature : Feature
    {
        return _features.OfType<TFeature>().FirstOrDefault()
            ?? throw new InvalidOperationException($"Feature of type {typeof(TFeature).Name} not found.");
    }

    private static async Task CheckForUpdates()
    {
        try
        {
            if (!App.UpdateManager.IsInstalled)
                return;

            UpdateInfo? updateInfo;
            using (Measurement.Operation("Checking for application updates"))
            {
                updateInfo = await App.UpdateManager.CheckForUpdatesAsync();
            }
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
                    using (Measurement.Operation("Downloading update"))
                    {
                        await App.UpdateManager.DownloadUpdatesAsync(updateInfo);
                    }
                    using (Measurement.Operation("Applying update"))
                    {
                        App.UpdateManager.ApplyUpdatesAndRestart(updateInfo);
                    }
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

        // Stop all local web app processes
        _localWebAppProcessManager.Dispose();

        LoggingService.SafeFlushLogsOnShutdown();

        base.OnClosing(e);
    }

    public void SetCurrentTab(TabBrowser? tab)
    {
        if (CurrentTab != null)
            CurrentTab.AddressChanged -= Tab_AddressChanged;

        WebContentBorder.Child = tab;
        CustomWindowChromeBrowserApi.ChangeAddress(GetAddressForPresentation(tab?.Address));

        if (tab != null)
            tab.AddressChanged += Tab_AddressChanged;
    }

    private void Tab_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        CustomWindowChromeBrowserApi.ChangeAddress(GetAddressForPresentation($"{e.NewValue}"));
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
        AnimateWindowBackgroundColor(newColor);
    }

    private static Color? _previousColor;
    private static void AnimateWindowBackgroundColor(Color contentColor)
    {
        if (_previousColor.HasValue)
        {
            Instance.AnimateBackgroundColor(_previousColor.Value, contentColor);
            _previousColor = contentColor;
        }
        else
        {
            Instance.SetBackgroundColor(contentColor);
            _previousColor = contentColor;
        }
    }

    // Shared helpers for side panel animations (ActionContext, TabPalette)
    internal void InitializeSidePanel(ColumnDefinition contentColumn, ColumnDefinition splitterColumn, double splitterExpandedWidth, bool isExpanded)
    {
        GridAnimationBehavior.Initialize(contentColumn);
        splitterColumn.Width = new GridLength(splitterExpandedWidth);
        GridAnimationBehavior.Initialize(splitterColumn);
        GridAnimationBehavior.SetIsExpanded(contentColumn, isExpanded);
        GridAnimationBehavior.SetIsExpanded(splitterColumn, isExpanded);
    }

    internal void ExpandSidePanel(ColumnDefinition contentColumn, ColumnDefinition splitterColumn, FrameworkElement contentElement, GridSplitter splitter, TimeSpan duration)
    {
        contentElement.Visibility = Visibility.Visible;
        splitter.Visibility = Visibility.Visible;
        GridAnimationBehavior.SetDuration(contentColumn, duration);
        GridAnimationBehavior.SetIsExpanded(contentColumn, true);
        GridAnimationBehavior.SetDuration(splitterColumn, duration);
        GridAnimationBehavior.SetIsExpanded(splitterColumn, true);
    }

    internal void CollapseSidePanel(ColumnDefinition contentColumn, ColumnDefinition splitterColumn, FrameworkElement contentElement, GridSplitter splitter, TimeSpan duration)
    {
        GridAnimationBehavior.SetDuration(contentColumn, duration);
        GridAnimationBehavior.SetIsExpanded(contentColumn, false);
        GridAnimationBehavior.SetDuration(splitterColumn, duration);
        GridAnimationBehavior.SetIsExpanded(splitterColumn, false);
        var timer = new DispatcherTimer { Interval = duration };
        timer.Tick += (s, e) =>
        {
            timer.Stop();
            contentElement.Visibility = Visibility.Collapsed;
            splitter.Visibility = Visibility.Collapsed;
        };
        timer.Start();
    }

    public void ShowTabPalette()
    {
        if (TabPaletteBrowserControl.Visibility == Visibility.Visible)
            return;

        var savedWidth = _appStateStateManager.GetAppState().TabPaletteWidth;
        TabPaletteColumn.Width = new GridLength(savedWidth > 0 ? savedWidth : 350);

        if (!_tabPaletteHasBeenShown)
        {
            InitializeSidePanel(TabPaletteColumn, TabPaletteSplitterColumn, 8, isExpanded: false);
            _tabPaletteHasBeenShown = true;
        }

        var duration = TimeSpan.FromMilliseconds(300);
        ExpandSidePanel(TabPaletteColumn, TabPaletteSplitterColumn, TabPaletteBrowserControl, TabPaletteGridSplitter, duration);
    }

    public void HideTabPalette()
    {
        if (TabPaletteBrowserControl.Visibility != Visibility.Visible)
            return;

        var duration = TimeSpan.FromMilliseconds(200);
        CollapseSidePanel(TabPaletteColumn, TabPaletteSplitterColumn, TabPaletteBrowserControl, TabPaletteGridSplitter, duration);
    }
}
