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
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Testably.Abstractions.Testing;

namespace BrowserHost.E2E.Infrastructure;

internal sealed class E2ETabBrowserHost : IDisposable
{
    private static readonly string _cefCachePath = Path.Combine(Path.GetTempPath(), "BrowserHost.E2E", $"CefCache-{Guid.NewGuid():N}");
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(4);

    private readonly Window _window;
    private readonly List<Feature> _features;
    private readonly E2EBrowserContext _context;
    private readonly PubSub _pubSub;

    public TabBrowser Tab { get; }
    public SettingsFeature Settings { get; }
    public PubSub PubSub => _pubSub;

    private E2ETabBrowserHost(Window window, TabBrowser tab, E2EBrowserContext context, List<Feature> features, SettingsFeature settings, PubSub pubSub)
    {
        _window = window;
        Tab = tab;
        _context = context;
        _features = features;
        Settings = settings;
        _pubSub = pubSub;
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

        var pubSub = new PubSub(new DispatcherPubSubDispatchStrategy(window.Dispatcher));
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

        // Note: we publish TabBrowserCreatedEvent after navigating to a content page in OpenSettingsPageAsync,
        // because this harness creates the initial tab as about:blank.

        window.Show();
        window.Activate();
        window.Dispatcher.Invoke(DispatcherPriority.Background, static () => { });

        ResetBrowserState(tab);

        return new E2ETabBrowserHost(window, tab, context, features, settingsFeature, pubSub);
    }

    private static void ResetBrowserState(TabBrowser tab)
    {
        // Ensure a known zoom baseline for the session/tab (CEF can retain per-origin zoom).
        tab.ResetZoomLevel();
    }

    public Task OpenSettingsPageAsync()
    {
        Tab.SetAddress(ContentServer.GetUiAddress("/settings"), setManualAddress: false);

        // Let SettingsFeature register the settingsApi bridge for this content page.
        _pubSub.Publish(new TabBrowserCreatedEvent(Tab));

        return WaitForSettingsPageReadyAsync();
    }

    public async Task WaitForSettingsPageReadyAsync(TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;

        await WaitForPageLoad(timeout);
        await WaitForJavascriptReadyAsync(timeout.Value);
        await WaitForConditionAsync(
            "document.querySelector('h1') && document.querySelector('h1').textContent.trim() === 'Settings'",
            timeout.Value);
        await WaitForConditionAsync(
            "document.querySelectorAll('.setting-row').length >= 3",
            timeout.Value);
    }

    public Task SetTextSettingAsync(string settingName, string value) =>
        ExecuteDomScriptAsync($@"(() => {{
            const rows = Array.from(document.querySelectorAll('.setting-row'));
            const row = rows.find(r => (r.querySelector('.text-sm.font-medium')?.textContent || '').trim() === {Json(settingName)});
            if (!row) throw new Error('Setting row not found: ' + {Json(settingName)});
            const input = row.querySelector('input[type=""text""]');
            if (!input) throw new Error('Text input not found for: ' + {Json(settingName)});
            input.focus();
            input.value = {Json(value)};
            input.dispatchEvent(new Event('input', {{ bubbles: true }}));
            input.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }})()");

    public Task SetCheckboxSettingAsync(string settingName, bool checkedValue) =>
        SetCheckboxSettingInternalAsync(settingName, checkedValue);

    private async Task SetCheckboxSettingInternalAsync(string settingName, bool checkedValue)
    {
        await WaitForJavascriptReadyAsync(_defaultTimeout);

        await ExecuteDomScriptAsync($@"(() => {{
            const rows = Array.from(document.querySelectorAll('.setting-row'));
            const row = rows.find(r => (r.querySelector('.text-sm.font-medium')?.textContent || '').trim() === {Json(settingName)});
            if (!row) throw new Error('Setting row not found: ' + {Json(settingName)});
            const input = row.querySelector('input[type=""checkbox""]');
            if (!input) throw new Error('Checkbox not found for: ' + {Json(settingName)});
            if (input.checked !== {Json(checkedValue)}) input.click();
        }})()");

        await WaitForConditionAsync($@"(() => {{
            const rows = Array.from(document.querySelectorAll('.setting-row'));
            const row = rows.find(r => (r.querySelector('.text-sm.font-medium')?.textContent || '').trim() === {Json(settingName)});
            const input = row && row.querySelector('input[type=""checkbox""]');
            return !!input && input.checked === {Json(checkedValue)};
        }})()", _defaultTimeout);
    }

    public async Task AddStringArrayItemAsync(string settingName, string value)
    {
        await WaitForJavascriptReadyAsync(_defaultTimeout);

        // Click Add
        await ExecuteDomScriptAsync($@"(() => {{
            const rows = Array.from(document.querySelectorAll('.setting-row'));
            const row = rows.find(r => (r.querySelector('.text-sm.font-medium')?.textContent || '').trim() === {Json(settingName)});
            if (!row) throw new Error('Setting row not found: ' + {Json(settingName)});
            const addBtn = Array.from(row.querySelectorAll('button')).find(b => (b.textContent || '').trim() === 'Add');
            if (!addBtn) throw new Error('Add button not found for: ' + {Json(settingName)});
            addBtn.click();
        }})()");

        // Wait for a new input to exist
        await WaitForConditionAsync($@"(() => {{
            const rows = Array.from(document.querySelectorAll('.setting-row'));
            const row = rows.find(r => (r.querySelector('.text-sm.font-medium')?.textContent || '').trim() === {Json(settingName)});
            return !!row && row.querySelectorAll('input[type=""text""]').length > 0;
        }})()", _defaultTimeout);

        // Type into the last input
        await ExecuteDomScriptAsync($@"(() => {{
            const rows = Array.from(document.querySelectorAll('.setting-row'));
            const row = rows.find(r => (r.querySelector('.text-sm.font-medium')?.textContent || '').trim() === {Json(settingName)});
            if (!row) throw new Error('Setting row not found: ' + {Json(settingName)});
            const inputs = Array.from(row.querySelectorAll('input[type=""text""]'));
            const input = inputs[inputs.length - 1];
            if (!input) throw new Error('Array item input not found after Add for: ' + {Json(settingName)});
            input.focus();
            input.value = {Json(value)};
            input.dispatchEvent(new Event('input', {{ bubbles: true }}));
            input.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }})()");
    }

    public async Task ClickButtonByTextAsync(string buttonText, TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;
        await WaitForJavascriptReadyAsync(timeout.Value);
        await WaitForConditionAsync($"Array.from(document.querySelectorAll('button')).some(b => (b.textContent||'').trim()==={Json(buttonText)} && !b.disabled)", timeout.Value);

        await ExecuteDomScriptAsync($@"(() => {{
            const btn = Array.from(document.querySelectorAll('button')).find(b => (b.textContent || '').trim() === {Json(buttonText)});
            if (!btn) throw new Error('Button not found: ' + {Json(buttonText)});
            btn.click();
        }})()");
    }

    public async Task WaitUntilAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        timeout ??= _defaultTimeout;
        var stopAt = DateTimeOffset.UtcNow + timeout.Value;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            if (condition()) return;
            await Task.Delay(50);
        }

        throw new TimeoutException("Condition not met within timeout.");
    }

    private ChromiumWebBrowser GetChromiumBrowser()
    {
        if (Tab.Content is not ChromiumWebBrowser browser)
            throw new InvalidOperationException("E2E DOM helpers require a ChromiumWebBrowser-backed tab.");
        return browser;
    }

    private async Task WaitForConditionAsync(string jsBooleanExpression, TimeSpan timeout)
    {
        await WaitForJavascriptReadyAsync(timeout);

        var stopAt = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < stopAt)
        {
            var ok = await EvaluateBoolAsync($"(() => {{ try {{ return !!({jsBooleanExpression}); }} catch (e) {{ return false; }} }})()")
                .ConfigureAwait(true);
            if (ok) return;
            await Task.Delay(50).ConfigureAwait(true);
        }

        throw new TimeoutException($"Timed out waiting for condition: {jsBooleanExpression}");
    }

    private async Task<bool> EvaluateBoolAsync(string script)
    {
        var browser = GetChromiumBrowser();
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

    private async Task WaitForJavascriptReadyAsync(TimeSpan timeout)
    {
        var browser = GetChromiumBrowser();
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

    private Task ExecuteDomScriptAsync(string script) => Tab.ExecuteScriptAsync(script);

    private static string Json(string value) => JsonSerializer.Serialize(value);
    private static string Json(bool value) => value ? "true" : "false";

    public async Task WaitForPageLoad(TimeSpan? timeout = null)
    {
        // For these E2E tests we avoid depending on external network/page load.
        // If the current tab isn't loading, give it a short grace period in case navigation hasn't started yet.
        if (!Tab.IsLoading)
        {
            var graceStopAt = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
            while (!Tab.IsLoading && DateTimeOffset.UtcNow < graceStopAt)
                await Task.Delay(10).ConfigureAwait(true);

            if (!Tab.IsLoading)
                return;
        }

        timeout ??= _defaultTimeout;
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        void Handler(object? s, EventArgs e)
        {
            Tab.PageLoadEnded -= Handler;
            tcs.TrySetResult();
        }

        Tab.PageLoadEnded += Handler;

        using var cts = new CancellationTokenSource(timeout.Value);
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
