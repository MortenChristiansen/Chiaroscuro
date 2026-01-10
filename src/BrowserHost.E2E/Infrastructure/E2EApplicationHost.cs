using BrowserHost.Features;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Settings;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using CefSharp;
using CefSharp.Wpf;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Testably.Abstractions.Testing;

namespace BrowserHost.E2E.Infrastructure;

internal sealed class E2EApplicationHost : IDisposable
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(4);
    private static readonly string _cefCachePath = Path.Combine(Path.GetTempPath(), "BrowserHost.E2E", $"CefCache-{Guid.NewGuid():N}");

    private readonly TaskCompletionSource _contentRenderedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _workspaceActivatedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly Window _window;
    private readonly E2EBrowserContext _context;

    public MainWindow MainWindow { get; }
    public SettingsFeature Settings { get; }
    public PubSub PubSub { get; }

    private E2EApplicationHost(Window window, MainWindow mainWindow, SettingsFeature settings, PubSub pubSub, E2EBrowserContext context)
    {
        _window = window;
        MainWindow = mainWindow;
        Settings = settings;
        PubSub = pubSub;
        _context = context;
    }

    public static E2EApplicationHost Create()
    {
        EnsureCefInitialized();

        // Use an explicit dispatch strategy; default dispatch uses MainWindow.Instance (not available yet).
        var pubSub = new PubSub(new InlinePubSubDispatchStrategy());

        var fileSystem = new MockFileSystem();
        var settingsFeature = new SettingsFeature(pubSub, new SettingsStateManager(fileSystem));

        var context = new E2EBrowserContext(settingsFeature);

        // Create the MainWindow with update-check and built-in content server disabled.
        var window = new MainWindow(settingsFeature, pubSub, fileSystem, browserContext: context, enableUpdateCheck: false, startContentServer: false)
        {
            Title = "BrowserHost.E2E",
            Width = 1200,
            Height = 800,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false,
        };

        var host = new E2EApplicationHost(window, window, settingsFeature, pubSub, context);

        window.ContentRendered += (_, __) => host._contentRenderedTcs.TrySetResult();

        // Track when the app has a workspace and when a settings tab is created.
        pubSub.Subscribe<WorkspaceActivatedEvent>(_ => host._workspaceActivatedTcs.TrySetResult());

        context.Attach(window);

        // Ensure PubSub dispatches onto this window's dispatcher.
        pubSub.DispatchStrategy = new DispatcherPubSubDispatchStrategy(window.Dispatcher);

        window.Show();
        window.Activate();
        window.Dispatcher.Invoke(DispatcherPriority.Background, static () => { });

        return host;
    }

    public async Task PressCtrlTAsync(TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;

        await WaitForAppReadyAsync(timeout);

        _context.SetTestKeyboardModifiers(ModifierKeys.Control);
        try
        {
            var source = PresentationSource.FromVisual(_window);
            var e = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.T)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
                Source = _window,
            };

            MainWindow.ProcessKeyboardEvent(e);
        }
        finally
        {
            _context.SetTestKeyboardModifiers(null);
        }

        await WaitForJavascriptReadyAsync(GetChromiumBrowser(MainWindow.ActionDialogUi), timeout.Value);
        await WaitForConditionAsync(GetChromiumBrowser(MainWindow.ActionDialogUi), "document.querySelector('input, textarea') != null", timeout.Value);
    }

    public async Task WaitForAppReadyAsync(TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;

        await WaitUntilAsync(() => _contentRenderedTcs.Task.IsCompleted, timeout);
        await WaitUntilAsync(() => _workspaceActivatedTcs.Task.IsCompleted, timeout);
    }

    public Task TypeInActionDialogAsync(string value) =>
        TypeInActionDialogInternalAsync(value);

    private async Task TypeInActionDialogInternalAsync(string value)
    {
        var browser = GetChromiumBrowser(MainWindow.ActionDialogUi);
        await WaitForActionDialogInputAsync(browser, _defaultTimeout);

        await EvaluateDomScriptAsync(browser, $@"(() => {{
            const input = document.querySelector('input[type=""text""], input:not([type]), textarea');
            if (!input) throw new Error('No input found in action dialog');
            input.focus();
            input.value = {Json(value)};
            input.dispatchEvent(new Event('input', {{ bubbles: true }}));
            input.dispatchEvent(new Event('change', {{ bubbles: true }}));
            return true;
        }})()", _defaultTimeout);
    }

    public Task PressEnterInActionDialogAsync() =>
        PressEnterInActionDialogInternalAsync();

    private async Task PressEnterInActionDialogInternalAsync()
    {
        var actionDialogBrowser = GetChromiumBrowser(MainWindow.ActionDialogUi);

        await WaitForActionDialogInputAsync(actionDialogBrowser, _defaultTimeout);

        await EvaluateDomScriptAsync(actionDialogBrowser, @"(() => {
            const input = document.querySelector('input[type=""text""], input:not([type]), textarea');
            if (!input) throw new Error('No input found in action dialog');
            const evt = new KeyboardEvent('keydown', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true });
            input.dispatchEvent(evt);
            const evtUp = new KeyboardEvent('keyup', { key: 'Enter', code: 'Enter', keyCode: 13, which: 13, bubbles: true });
            input.dispatchEvent(evtUp);
            return true;
        })()", _defaultTimeout);

        var value = await EvaluateStringAsync(actionDialogBrowser, @"(() => {
            const input = document.querySelector('input[type=""text""], input:not([type]), textarea');
            return input ? (input.value || '') : '';
        })()");

        // Deterministic path: mimic the frontend calling ActionDialogBackendApi.Execute(...)
        PubSub.Send(new ExecuteCommandCommand(value, Ctrl: false));
    }

    private static async Task WaitForActionDialogInputAsync(ChromiumWebBrowser browser, TimeSpan timeout)
    {
        await WaitForJavascriptReadyAsync(browser, timeout);

        // In some runners (notably VS), the browser can still be at about:blank briefly.
        var stopAt = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var address = await browser.Dispatcher.InvokeAsync(() => browser.Address);
            if (!string.IsNullOrWhiteSpace(address) && !string.Equals(address, "about:blank", StringComparison.OrdinalIgnoreCase))
                break;

            await Task.Delay(50).ConfigureAwait(true);
        }

        await WaitForConditionAsync(
            browser,
            "document.querySelector('input[type=\\\"text\\\"], input:not([type]), textarea') != null",
            timeout);
    }

    private static async Task<string> EvaluateStringAsync(ChromiumWebBrowser browser, string script)
    {
        var response = await browser.Dispatcher
            .InvokeAsync(() => browser.EvaluateScriptAsync(script))
            .Task
            .Unwrap();

        if (!response.Success || response.Result is null)
            return string.Empty;

        return response.Result.ToString() ?? string.Empty;
    }

    public async Task WaitForSettingsPageReadyAsync(TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;

        await WaitForAppReadyAsync(timeout);

        // In some cases the created tab may start as about:blank and navigate afterwards,
        // so relying on TabBrowserCreatedEvent to capture the final address is flaky.
        await WaitUntilAsync(() => MainWindow.CurrentTab != null && IsSettingsAddress(MainWindow.CurrentTab.Address), timeout);

        var tab = MainWindow.CurrentTab!;
        await WaitForJavascriptReadyAsync(GetChromiumBrowser(tab), timeout.Value);
        await WaitForConditionAsync(GetChromiumBrowser(tab), "document.querySelector('h1') && document.querySelector('h1').textContent.trim() === 'Settings'", timeout.Value);
    }

    private static bool IsSettingsAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return false;

        var normalized = address.TrimEnd('/');
        return normalized.EndsWith("/settings", StringComparison.OrdinalIgnoreCase);
    }

    public async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;
        var stopAt = DateTimeOffset.UtcNow + timeout.Value;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            if (condition())
                return;
            await Task.Delay(50).ConfigureAwait(true);
        }

        throw new TimeoutException("Condition not met within timeout.");
    }

    private static ChromiumWebBrowser GetChromiumBrowser(object browser)
    {
        if (browser is TabBrowser tabBrowser)
        {
            if (tabBrowser.Content is ChromiumWebBrowser tabCef)
                return tabCef;

            throw new InvalidOperationException("Expected TabBrowser to be backed by ChromiumWebBrowser.");
        }

        if (browser is ChromiumWebBrowser b)
            return b;

        throw new InvalidOperationException("Expected a ChromiumWebBrowser-backed UI browser.");
    }

    private static async Task EvaluateDomScriptAsync(ChromiumWebBrowser browser, string script, TimeSpan timeout)
    {
        await WaitForJavascriptReadyAsync(browser, timeout);

        var response = await browser.Dispatcher
            .InvokeAsync(() => browser.EvaluateScriptAsync(script))
            .Task
            .Unwrap();

        if (!response.Success)
            throw new InvalidOperationException($"Script evaluation failed: {response.Message}");
    }

    private static async Task WaitForConditionAsync(ChromiumWebBrowser browser, string jsBooleanExpression, TimeSpan timeout)
    {
        await WaitForJavascriptReadyAsync(browser, timeout);

        var stopAt = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var ok = await EvaluateBoolAsync(browser, $"(() => {{ try {{ return !!({jsBooleanExpression}); }} catch (e) {{ return false; }} }})()")
                .ConfigureAwait(true);
            if (ok)
                return;

            await Task.Delay(50).ConfigureAwait(true);
        }

        throw new TimeoutException($"Timed out waiting for condition: {jsBooleanExpression}");
    }

    private static async Task<bool> EvaluateBoolAsync(ChromiumWebBrowser browser, string script)
    {
        try
        {
            var response = await browser.Dispatcher
                .InvokeAsync(() => browser.EvaluateScriptAsync(script))
                .Task
                .Unwrap();

            if (!response.Success)
                return false;

            if (response.Result is bool b)
                return b;

            return response.Result != null && response.Result.ToString() == "true";
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForJavascriptReadyAsync(ChromiumWebBrowser browser, TimeSpan timeout)
    {
        var stopAt = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < stopAt)
        {
            var canExecute = await browser.Dispatcher.InvokeAsync(() => browser.CanExecuteJavascriptInMainFrame);
            if (canExecute)
                return;

            await Task.Delay(50).ConfigureAwait(true);
        }

        throw new TimeoutException("Timed out waiting for JavaScript context to become available.");
    }

    private static string Json(string value) => JsonSerializer.Serialize(value);


    public void Dispose()
    {
        try { _window.Dispatcher.Invoke(_window.Close); } catch { }
    }

    private sealed class InlinePubSubDispatchStrategy : PubSub.IPubSubDispatchStrategy
    {
        public void Invoke<T>(Action<T> action, T message) => action(message);
        public Task InvokeAsync<T>(Func<T, Task> action, T message) => action(message);
    }

    private sealed class DispatcherPubSubDispatchStrategy(Dispatcher dispatcher) : PubSub.IPubSubDispatchStrategy
    {
        private readonly Dispatcher _dispatcher = dispatcher;

        public void Invoke<T>(Action<T> action, T message)
        {
            if (_dispatcher.CheckAccess())
            {
                action(message);
                return;
            }

            _dispatcher.Invoke(() => action(message));
        }

        public Task InvokeAsync<T>(Func<T, Task> action, T message)
        {
            if (_dispatcher.CheckAccess())
                return action(message);

            return _dispatcher.InvokeAsync(() => action(message)).Task.Unwrap();
        }
    }

    private sealed class E2EBrowserContext(SettingsFeature settingsFeature) : IBrowserContext
    {
        private readonly SettingsFeature _settingsFeature = settingsFeature;
        private MainWindow _window = null!;

        public void Attach(MainWindow window)
        {
            _window = window;
            TabsHost = new MainWindowTabsHost(_window);
        }

        private ModifierKeys? _testKeyboardModifiers;
        public void SetTestKeyboardModifiers(ModifierKeys? modifiers) => _testKeyboardModifiers = modifiers;

        public Options AppOptions => new();

        public ITabBrowser? CurrentTab => _window.CurrentTab;
        public string? CurrentTabId => _window.CurrentTab?.Id;

        public ModifierKeys CurrentKeyboardModifiers => _testKeyboardModifiers ?? Keyboard.Modifiers;

        public System.Windows.Media.Color WorkspaceColor
        {
            get => _window.WorkspaceColor;
            set => _window.WorkspaceColor = value;
        }

        public WindowState WindowState
        {
            get => _window.WindowState;
            set => _window.WindowState = value;
        }

        public IDragDropHost DragDropHost { get; } = new NullDragDropHost();
        public ITabsHost TabsHost { get; private set; } = null!;

        public void SetClipboardText(string text) => Clipboard.SetText(text);

        public void SetCurrentTab(ITabBrowser? tab)
        {
            if (tab is null)
            {
                _window.SetCurrentTab(null);
                return;
            }

            if (tab is not TabBrowser tabBrowser)
                throw new InvalidOperationException("Cannot set a non-TabBrowser instance as current tab.");

            _window.SetCurrentTab(tabBrowser);
        }

        public ITabBrowser CreateNewTab(string address, global::BrowserHost.Features.ActionContext.Tabs.TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
            CreateExistingTab($"{Guid.NewGuid()}", address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser);

        public ITabBrowser CreateExistingTab(string tabId, string address, global::BrowserHost.Features.ActionContext.Tabs.TabsBrowserApi tabsApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
            new TabBrowser(tabId, address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser, _settingsFeature);

        public bool ActionRequiresDispatch => !_window.Dispatcher.CheckAccess();

        public void Dispatch(Action action) => _window.Dispatcher.Invoke(action);

        public TFeature GetFeature<TFeature>() where TFeature : Feature => _window.GetFeature<TFeature>();

        private sealed class NullDragDropHost : global::BrowserHost.Features.DragDrop.IDragDropHost
        {
            public bool AllowDrop { get; set; }
            public event Action? DragEnter;
            public event Action? DragLeave;
            public event Action<string[]>? FilesDropped;
        }

    }

    private static void EnsureCefInitialized()
    {
#if ANYCPU
        CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif

        if (Cef.IsInitialized != true)
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
}
