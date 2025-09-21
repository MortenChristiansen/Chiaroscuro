using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.Notifications;
using BrowserHost.Utilities;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace BrowserHost.Tab.WebView2;

public sealed class WebView2Browser : UserControl, ITabWebBrowser, IDisposable
{
    private static readonly Lazy<Task<CoreWebView2Environment>> _environment = new(CreateEnvironment);
    private readonly string _id;
    private readonly string? _initialManualAddress;
    private readonly string? _initialFavicon;
    private string? _manualAddress;
    private string? _favicon;
    private string _title = string.Empty;
    private bool _isLoading;
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _core;

    private const int CornerRadiusPx = 8; // Match CefSharp visual
    private readonly Border _hostSurface = new()
    {
        Background = Brushes.Transparent,
        CornerRadius = new CornerRadius(CornerRadiusPx)
    };

    private string? _pendingNavigateTo;
    private string? _lastAddressSnapshot;
    private double _zoomFactor = 1.0;
    private readonly WebView2SnapshotOverlay _snapshotOverlay = new();
    private readonly WebView2FindManager _findManager = new();
    private readonly WebView2RoundedCornerManager _roundedCornerManager = new(CornerRadiusPx);

    // Cache of last applied bounds to avoid redundant work
    private int _lastX = -1, _lastY = -1, _lastW = -1, _lastH = -1;

    private static readonly DependencyProperty AddressProperty = DependencyProperty.Register(
        nameof(Address), typeof(string), typeof(WebView2Browser), new PropertyMetadata(string.Empty));

    public event DependencyPropertyChangedEventHandler? AddressChanged;

    public WebView2Browser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon)
    {
        _id = id;
        _initialManualAddress = setManualAddress ? address : null;
        _initialFavicon = favicon;
        _manualAddress = _initialManualAddress;
        _pendingNavigateTo = NormalizeAddress(address);

        _hostSurface.Child = _snapshotOverlay.Visual;

        _hostSurface.Loaded += async (_, _) => { await EnsureControllerAsync(actionContextBrowser); SyncControllerVisibility(); };
        _hostSurface.Unloaded += (_, _) => SyncControllerVisibility();
        _hostSurface.IsVisibleChanged += (_, _) => SyncControllerVisibility();
        _hostSurface.SizeChanged += (_, _) => { UpdateControllerBounds(); };
        _hostSurface.LayoutUpdated += (_, _) => { if (_hostSurface.IsVisible) UpdateControllerBounds(); };

        PubSub.Subscribe<ActionDialogShownEvent>(HandleActionDialogShownEvent);
        PubSub.Subscribe<ActionDialogDismissedEvent>(HandleActionDialogDismissedEvent);
    }

    public string Id => _id;
    public string? Favicon => _favicon ?? _initialFavicon;
    public string? ManualAddress => _manualAddress ?? _initialManualAddress;
    public string Address => RunOnUi(() => _core?.Source ?? _pendingNavigateTo ?? string.Empty);
    public string Title { get => _title; set => _title = value; }
    public bool IsLoading => _isLoading;
    public bool CanGoBack => RunOnUi(() => _core?.CanGoBack ?? false);
    public bool CanGoForward => RunOnUi(() => _core?.CanGoForward ?? false);
    public bool HasDevTools => false;
    public double DefaultZoomLevel => 1.0;

    private void HandleActionDialogShownEvent(ActionDialogShownEvent _)
    {
        if (_hostSurface.IsVisible)
            ActivateSnapshotAsync();
    }

    private void HandleActionDialogDismissedEvent(ActionDialogDismissedEvent _)
    {
        if (_snapshotOverlay.IsActive)
            DeactivateSnapshot();
    }

    private static async Task<CoreWebView2Environment> CreateEnvironment()
    {
        var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebView2LowLevelCache");
        Directory.CreateDirectory(userDataFolder);
        var options = new CoreWebView2EnvironmentOptions
        {
            AllowSingleSignOnUsingOSPrimaryAccount = true,
        };
        return await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder, options: options);
    }

    private async Task EnsureControllerAsync(ActionContextBrowser actionContextBrowser)
    {
        if (_controller != null) return;
        var parentWindow = Window.GetWindow(_hostSurface);
        if (parentWindow == null) return;
        var parentHwnd = new WindowInteropHelper(parentWindow).Handle;
        _roundedCornerManager.SetParentWindowHandle(parentHwnd);
        var env = await _environment.Value;
        _controller = await env.CreateCoreWebView2ControllerAsync(parentHwnd);
        _core = _controller.CoreWebView2;
        _findManager.Initialize(_core);
        UpdateControllerBounds();
        WireCoreEvents(actionContextBrowser);
        ApplySettings();
        if (_pendingNavigateTo != null) { _core.Navigate(_pendingNavigateTo); _pendingNavigateTo = null; }
        _roundedCornerManager.EnsureChildWindowAsync(Dispatcher, () => _controller?.Bounds.Width ?? 0, () => _controller?.Bounds.Height ?? 0);
    }

    private List<IDisposable> _handlers = [];
    private void WireCoreEvents(ActionContextBrowser actionContextBrowser)
    {
        if (_core == null) return;
        _core.NavigationStarting += (_, __) =>
        {
            _isLoading = true;
            PubSub.Publish(new TabLoadingStateChangedEvent(_id, true));
        };
        _core.NavigationCompleted += (_, args) =>
        {
            _isLoading = false;
            PubSub.Publish(new TabLoadingStateChangedEvent(_id, false));
            var newAddress = _core.Source;
            if (_lastAddressSnapshot != newAddress)
            {
                var previous = _lastAddressSnapshot;
                _lastAddressSnapshot = newAddress;
                AddressChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(AddressProperty, previous, newAddress));
            }
        };
        _core.DocumentTitleChanged += (_, __) => { _title = _core.DocumentTitle; actionContextBrowser.UpdateTabTitle(_id, _title); };
        _core.FaviconChanged += (_, __) => { _favicon = _core.FaviconUri; actionContextBrowser.UpdateTabFavicon(_id, _favicon); };
        // Mimic CefSharp RequestHandler: open new window requests as background tabs instead of OS windows
        _core.NewWindowRequested += Core_NewWindowRequested;
        _controller!.AcceleratorKeyPressed += Controller_AcceleratorKeyPressed;

        _handlers.Add(WebViewPermissionHandler.Register(_core));
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var uri = e.Uri;
        if (!string.IsNullOrEmpty(uri))
        {
            PubSub.Publish(new NavigationStartedEvent(uri, UseCurrentTab: false, SaveInHistory: true));
            e.Handled = true; // Prevent external window
        }
    }

    private void ApplySettings()
    {
        if (_core == null) return;
        var settings = _core.Settings;
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = true;
        if (_controller != null)
        {
            try { _controller.ZoomFactor = _zoomFactor; } catch { }
            try { _controller.DefaultBackgroundColor = System.Drawing.Color.Transparent; } catch { }
        }
    }

    private static readonly HashSet<Key> _allowedCtrlEditingKeys = [Key.C, Key.V, Key.X, Key.A, Key.Z, Key.Y, Key.Delete, Key.Left, Key.Right, Key.Back];
    private void Controller_AcceleratorKeyPressed(object? sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
    {
        if (e.KeyEventKind is not (CoreWebView2KeyEventKind.KeyDown or CoreWebView2KeyEventKind.SystemKeyDown)) return;
        var key = KeyInterop.KeyFromVirtualKey((int)e.VirtualKey);
        var mods = Keyboard.Modifiers;
        var ctrl = (mods & ModifierKeys.Control) != 0;
        var alt = (mods & ModifierKeys.Alt) != 0;
        var forward = false;
        if (alt || key is Key.F5 or Key.F12 || (ctrl && (!_allowedCtrlEditingKeys.Contains(key) || key == Key.Tab))) forward = true;
        if (forward)
        {
            e.Handled = true;
            var source = PresentationSource.FromVisual(MainWindow.Instance);
            if (source != null)
            {
                var args = new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                MainWindow.Instance.ProcessKeyboardEvent(args);
            }
        }
    }

    private void SyncControllerVisibility()
    {
        if (_controller == null) return;
        var shouldBeVisible = _hostSurface.IsVisible && !_snapshotOverlay.IsActive;
        if (_controller.IsVisible != shouldBeVisible)
            _controller.IsVisible = shouldBeVisible;
        if (shouldBeVisible)
            UpdateControllerBounds();
    }

    private void UpdateControllerBounds()
    {
        if (_controller == null || _snapshotOverlay.IsActive || !_hostSurface.IsVisible) return;
        var window = Window.GetWindow(_hostSurface);
        if (window == null) return;
        var topLeft = _hostSurface.TranslatePoint(new Point(0, 0), window);
        var size = new Size(_hostSurface.ActualWidth, _hostSurface.ActualHeight);
        if (size.Width <= 0 || size.Height <= 0) return;
        var ps = PresentationSource.FromVisual(window);
        var dpiX = ps?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var dpiY = ps?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        var x = (int)Math.Round(topLeft.X * dpiX);
        var y = (int)Math.Round(topLeft.Y * dpiY);
        var w = (int)Math.Round(size.Width * dpiX);
        var h = (int)Math.Round(size.Height * dpiY);
        // Skip if bounds unchanged
        if (x == _lastX && y == _lastY && w == _lastW && h == _lastH) return;
        _lastX = x; _lastY = y; _lastW = w; _lastH = h;
        _controller.Bounds = new System.Drawing.Rectangle(x, y, w, h);
        _roundedCornerManager.ApplyRoundedRegion(w, h);
    }

    private async void ActivateSnapshotAsync()
    {
        if (_controller == null || _core == null) return;
        var activated = await _snapshotOverlay.TryActivateAsync(_core);
        if (activated)
        {
            _controller.IsVisible = false;
            SyncControllerVisibility();
        }
    }

    private void DeactivateSnapshot()
    {
        _snapshotOverlay.Deactivate();
        SyncControllerVisibility();
    }

    public void SetAddress(string address, bool setManualAddress)
    {
        var normalized = NormalizeAddress(address);
        if (setManualAddress) _manualAddress = address;
        if (_core == null) { _pendingNavigateTo = normalized; return; }
        if (normalized != null) _core.Navigate(normalized);
    }

    private void RunOnUi(Action action)
    {
        if (Dispatcher.CheckAccess()) action();
        else Dispatcher.Invoke(action);
    }

    private T RunOnUi<T>(Func<T> action)
    {
        if (Dispatcher.CheckAccess()) return action();
        else return Dispatcher.Invoke(action);
    }

    public void RegisterContentPageApi(BrowserApi api, string name) => throw new InvalidOperationException("The WebView2Browser does not support content pages");
    public void Reload(bool ignoreCache = false) => RunOnUi(() => _core?.Reload());
    public void Back() { if (CanGoBack) RunOnUi(() => _core?.GoBack()); }
    public void Forward() { if (CanGoForward) RunOnUi(() => _core?.GoForward()); }
    public async Task CallClientApi(string api, string? arguments = null) { if (_core != null) await _core.ExecuteScriptAsync($"{api}({arguments ?? string.Empty});"); }
    public async Task ExecuteScriptAsync(string script) { if (_core != null) await _core.ExecuteScriptAsync(script); }
    public Task<double> GetZoomLevelAsync() => Task.FromResult(_controller?.ZoomFactor ?? _zoomFactor);

    public void SetZoomLevel(double level)
    {
        var clamped = Math.Clamp(level, 0.25, 5.0);
        _zoomFactor = clamped;
        if (_controller != null)
        {
            try { _controller.ZoomFactor = clamped; } catch { }
        }
    }

    public void Find(string searchText, bool forward, bool matchCase, bool findNext) => _findManager.Find(searchText, forward, matchCase, findNext);
    public void StopFinding(bool clearSelection) => _findManager.StopFinding(clearSelection);

    public UIElement AsUIElement() => _hostSurface;
    public void ShowDevTools() => _core?.OpenDevToolsWindow();
    public void CloseDevTools() { }

    public void Dispose()
    {
        try
        {
            PubSub.Unsubscribe<ActionDialogShownEvent>(HandleActionDialogShownEvent);
            PubSub.Unsubscribe<ActionDialogDismissedEvent>(HandleActionDialogDismissedEvent);
            if (_core != null)
            {
                _handlers.ForEach(h => h.Dispose());
                _core.NewWindowRequested -= Core_NewWindowRequested;
                _core = null;
            }
            if (_controller != null)
            {
                _controller.AcceleratorKeyPressed -= Controller_AcceleratorKeyPressed;
                _controller.Close();
                _controller = null;
            }
        }
        catch { }
    }

    private static string? NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Scheme)) return "https://" + address;
        return address;
    }
}
