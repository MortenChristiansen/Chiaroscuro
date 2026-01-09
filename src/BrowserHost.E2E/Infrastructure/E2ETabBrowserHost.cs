using BrowserHost.Features;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using CefSharp;
using CefSharp.Wpf;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Testably.Abstractions.Testing;

namespace BrowserHost.E2E.Infrastructure;

internal sealed class E2ETabBrowserHost : IDisposable
{
    private static readonly string _cefCachePath = Path.Combine(Path.GetTempPath(), "BrowserHost.E2E", $"CefCache-{Guid.NewGuid():N}");

    private readonly Window _window;
    private readonly List<Feature> _features;
    private readonly E2EBrowserContext _context;

    public TabBrowser Tab { get; }

    private E2ETabBrowserHost(Window window, TabBrowser tab, E2EBrowserContext context, List<Feature> features)
    {
        _window = window;
        Tab = tab;
        _context = context;
        _features = features;
    }

    public static E2ETabBrowserHost Create()
    {
        EnsureCefInitialized();

        var window = new Window
        {
            Title = "BrowserHost.E2E",
            Width = 1200,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false,
        };

        var root = new Grid();
        window.Content = root;

        var pubSub = new PubSub();
        var fileSystem = new MockFileSystem();
        var settingsFeature = new SettingsFeature(pubSub, new SettingsStateManager(fileSystem));

        var tabsApi = new NullTabsBrowserApi();

        var context = new E2EBrowserContext(window, settingsFeature)
        {
            PubSub = pubSub,
            TabsApi = tabsApi,
        };

        var tab = new TabBrowser($"tab-{Guid.NewGuid()}", "about:blank", tabsApi, pubSub, setManualAddress: false, favicon: null, isChildBrowser: false, settingsFeature);
        context.SetCurrentTab(tab);
        root.Children.Add(tab);

        var features = new List<Feature>
        {
            settingsFeature,
            new ZoomFeature(pubSub, context),
        };

        foreach (var f in features)
        {
            context.RegisterFeatureInstance(f);
            f.Configure();
        }

        window.Show();
        window.Activate();
        window.Dispatcher.Invoke(DispatcherPriority.Background, static () => { });

        // Ensure a known zoom baseline for the session/tab (CEF can retain per-origin zoom).
        tab.ResetZoomLevel();

        return new E2ETabBrowserHost(window, tab, context, features);
    }

    public async Task WaitForPageLoad(TimeSpan? timeout = null)
    {
        // For these E2E tests we avoid depending on external network/page load.
        // If the current tab isn't loading, return immediately.
        if (!Tab.IsLoading)
            return;

        timeout ??= TimeSpan.FromSeconds(30);
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? s, EventArgs e)
        {
            Tab.PageLoadEnded -= Handler;
            tcs.TrySetResult();
        }

        Tab.PageLoadEnded += Handler;

        using var cts = new System.Threading.CancellationTokenSource(timeout.Value);
        cts.Token.Register(() =>
        {
            Tab.PageLoadEnded -= Handler;
            tcs.TrySetCanceled(cts.Token);
        });

        await tcs.Task;
    }

    public void SimulateMouseWheel(int delta, ModifierKeys modifiers)
    {
        _context.SetTestKeyboardModifiers(modifiers);
        try
        {
            var e = new MouseWheelEventArgs(InputManager.Current.PrimaryMouseDevice, Environment.TickCount, delta)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent,
                Source = Tab,
            };

            Tab.Dispatcher.Invoke(() => Tab.RaiseEvent(e));

            Tab.Dispatcher.Invoke(() =>
            {
                // Mirror how the app routes to features; for this E2E we only register ZoomFeature.
                foreach (var feature in _features)
                {
                    if (feature.HandleOnPreviewMouseWheel(e))
                    {
                        e.Handled = true;
                        break;
                    }
                }
            });
        }
        finally
        {
            _context.SetTestKeyboardModifiers(null);
        }
    }

    public void Dispose()
    {
        try { _window.Dispatcher.Invoke(() => _window.Close()); } catch { }
    }

    private static void EnsureCefInitialized()
    {
#if ANYCPU
        CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

        if (Cef.IsInitialized == null)
        {
            Directory.CreateDirectory(_cefCachePath);

            var baseDir = AppContext.BaseDirectory;

            var settings = new CefSettings
            {
                CachePath = _cefCachePath,
                ResourcesDirPath = baseDir,
                LocalesDirPath = Path.Combine(baseDir, "locales"),
                BrowserSubprocessPath = Path.Combine(baseDir, "CefSharp.BrowserSubprocess.exe"),
            };

            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }
    }

    private sealed class E2EBrowserContext(Window window, SettingsFeature settingsFeature) : IBrowserContext
    {
        private readonly Window _window = window;
        private readonly SettingsFeature _settingsFeature = settingsFeature;
        private readonly Dictionary<Type, Feature> _features = [];

        public PubSub PubSub { get; init; } = null!;
        public TabsBrowserApi TabsApi { get; init; } = null!;

        public Options AppOptions => new();
        public ITabBrowser? CurrentTab { get; private set; }
        public string? CurrentTabId => CurrentTab?.Id;

        private ModifierKeys? _testKeyboardModifiers;
        public void SetTestKeyboardModifiers(ModifierKeys? modifiers) => _testKeyboardModifiers = modifiers;
        public ModifierKeys CurrentKeyboardModifiers => _testKeyboardModifiers ?? Keyboard.Modifiers;

        public System.Windows.Media.Color WorkspaceColor { get; set; } = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
        public WindowState WindowState { get => _window.WindowState; set => _window.WindowState = value; }

        public IDragDropHost DragDropHost { get; } = new NullDragDropHost();
        public ITabsHost TabsHost { get; } = new NullTabsHost();

        public void SetClipboardText(string text) => Clipboard.SetText(text);

        public void SetCurrentTab(ITabBrowser? tab) => CurrentTab = tab;

        public ITabBrowser CreateNewTab(string address, TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
            CreateExistingTab($"{Guid.NewGuid()}", address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser);

        public ITabBrowser CreateExistingTab(string tabId, string address, TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
            new TabBrowser(tabId, address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser, _settingsFeature);

        public bool ActionRequiresDispatch => !_window.Dispatcher.CheckAccess();

        public void Dispatch(Action action) => _window.Dispatcher.Invoke(action);

        public TFeature GetFeature<TFeature>() where TFeature : Feature =>
            _features.TryGetValue(typeof(TFeature), out var f) ? (TFeature)f : throw new InvalidOperationException($"Feature of type {typeof(TFeature).Name} not found");

        public void RegisterFeatureInstance(Feature feature) => _features[feature.GetType()] = feature;
    }

    private sealed class NullDragDropHost : IDragDropHost
    {
        public bool AllowDrop { get; set; }
        public event Action? DragEnter;
        public event Action? DragLeave;
        public event Action<string[]>? FilesDropped;
    }

    private sealed class NullTabsHost : ITabsHost
    {
        public void PreloadTab(ITabBrowser tab) { }
        public void RemovePreloadedTab(String tabId) { }
    }

    private sealed class NullTabsBrowserApi : TabsBrowserApi
    {
        public NullTabsBrowserApi() : base(null!) { }
        public override void CallClientApi(string api, string? arguments = null) { }
    }
}
