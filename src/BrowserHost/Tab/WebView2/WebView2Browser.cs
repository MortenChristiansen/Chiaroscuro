using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.Permissions;
using BrowserHost.Features.WebContextMenu;
using BrowserHost.Utilities;
using BrowserHost.XamlUtilities;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
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
    private bool _isChildBrowser;
    private string? _manualAddress;
    private string? _favicon;
    private string _title = string.Empty;
    private bool _isLoading;
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _core;

    private nint _currentParentWindowHandle;
    private nint _forcedParentWindowHandle;

    private const int CornerRadiusPx = 8; // Match CefSharp visual
    private readonly ActionContextBrowser _actionContextBrowser;
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
        nameof(Address), typeof(string), typeof(WebView2Browser), new PropertyMetadata(string.Empty)
    );

    public event DependencyPropertyChangedEventHandler? AddressChanged;
    public event EventHandler? PageLoadEnded;

    public WebView2Browser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        _id = id;
        _initialManualAddress = setManualAddress ? address : null;
        _initialFavicon = favicon;
        _isChildBrowser = isChildBrowser;
        _manualAddress = _initialManualAddress;
        _pendingNavigateTo = NormalizeAddress(address);
        _actionContextBrowser = actionContextBrowser;

        _hostSurface.Child = _snapshotOverlay.Visual;

        _hostSurface.Loaded += async (_, _) => { await EnsureControllerAsync(); SyncControllerVisibility(); };
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

    private async Task EnsureControllerAsync()
    {
        var parentWindow = Window.GetWindow(_hostSurface);
        if (parentWindow == null) return;
        var detectedParentHwnd = new WindowInteropHelper(parentWindow).Handle;
        var targetParentHwnd = _forcedParentWindowHandle != IntPtr.Zero ? _forcedParentWindowHandle : detectedParentHwnd;
        if (targetParentHwnd == IntPtr.Zero) return;

        _roundedCornerManager.SetParentWindowHandle(targetParentHwnd);

        if (_controller != null)
        {
            TryReparentController(targetParentHwnd);
            // The controller may have been reparented while this control was detached (promotion from child window).
            // Force a bounds refresh now that we're attached to the correct visual tree.
            _lastX = _lastY = _lastW = _lastH = -1;
            SyncControllerVisibility();
            UpdateControllerBounds();
            return;
        }
        var env = await _environment.Value;
        _controller = await env.CreateCoreWebView2ControllerAsync(targetParentHwnd);
        _core = _controller.CoreWebView2;
        _currentParentWindowHandle = targetParentHwnd;
        _findManager.Initialize(_core);
        UpdateControllerBounds();
        WireCoreEvents();
        ApplySettings();
        if (_pendingNavigateTo != null) { _core.Navigate(_pendingNavigateTo); _pendingNavigateTo = null; }
        _roundedCornerManager.EnsureChildWindowAsync(Dispatcher, () => _controller?.Bounds.Width ?? 0, () => _controller?.Bounds.Height ?? 0);
    }

    private void TryReparentController(nint newParentWindowHandle)
    {
        if (_controller == null) return;
        if (newParentWindowHandle == IntPtr.Zero) return;
        if (_currentParentWindowHandle == newParentWindowHandle) return;

        try
        {
            _controller.ParentWindow = newParentWindowHandle;
            _currentParentWindowHandle = newParentWindowHandle;
        }
        catch
        {
            // If WebView2 runtime doesn't allow reparenting here, we fall back to existing parent.
        }

        try { _controller.NotifyParentWindowPositionChanged(); } catch { }
        UpdateControllerBounds();
    }

    private readonly List<IDisposable> _handlers = [];
    private void WireCoreEvents()
    {
        if (_core == null) return;
        _core.NavigationStarting += Core_NavigationStarting;
        _core.NavigationCompleted += Core_NavigationCompleted;
        _core.ContextMenuRequested += Core_ContextMenuRequested;
        if (!_isChildBrowser)
        {
            _core.DocumentTitleChanged += Core_DocumentTitleChanged;
            _core.FaviconChanged += Core_FaviconChanged;
        }
        // Mimic CefSharp RequestHandler: open new window requests as background tabs instead of OS windows
        _core.NewWindowRequested += Core_NewWindowRequested;
        _controller!.AcceleratorKeyPressed += Controller_AcceleratorKeyPressed;

        _handlers.Add(WebViewPermissionHandler.Register(_core));
    }

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        _isLoading = true;
        PubSub.Publish(new TabLoadingStateChangedEvent(_id, true));
    }

    private void Core_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        _isLoading = false;
        PubSub.Publish(new TabLoadingStateChangedEvent(_id, false));
        var newAddress = _core?.Source;
        if (_lastAddressSnapshot != newAddress)
        {
            var previous = _lastAddressSnapshot;
            _lastAddressSnapshot = newAddress;
            AddressChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(AddressProperty, previous, newAddress));
        }
        PageLoadEnded?.Invoke(this, EventArgs.Empty);
    }

    private void Core_DocumentTitleChanged(object? sender, object e)
    {
        if (_core == null) return;
        _title = _core.DocumentTitle;
        _actionContextBrowser.UpdateTabTitle(_id, _title);
    }

    private void Core_FaviconChanged(object? sender, object e)
    {
        if (_core == null) return;
        _favicon = _core.FaviconUri;
        _actionContextBrowser.UpdateTabFavicon(_id, _favicon);
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        var uri = e.Uri;
        if (string.IsNullOrEmpty(uri)) return;

        var isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        var isMiddleClick = Mouse.MiddleButton == MouseButtonState.Pressed; // This does not seem to work reliably if at all
        if (isCtrlPressed || isMiddleClick)
        {
            // Ctrl+click or middle-click -> open in background tab
            e.Handled = true;
            PubSub.Publish(new NavigationStartedEvent(uri, UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));
            return;
        }
        else
        {
            e.Handled = true;
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                var owner = MainWindow.Instance;
                var parentTabId = !_isChildBrowser ? _id : (MainWindow.Instance.CurrentTab?.Id ?? _id);
                var win = new ChildBrowserWindow(uri, parentTabId) { Owner = owner };
                win.Show();
            });
            return;
        }
    }

    private void ApplySettings()
    {
        if (_core == null) return;
        var settings = _core.Settings;
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = true;
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

        // Use screen pixel coordinates to avoid DPI/transform pitfalls.
        // CoreWebView2Controller.Bounds is in pixels relative to the parent window client area.
        Point hostTopLeftPx;
        Point hostBottomRightPx;
        Point windowClientTopLeftPx;
        try
        {
            hostTopLeftPx = _hostSurface.PointToScreen(new Point(0, 0));
            hostBottomRightPx = _hostSurface.PointToScreen(new Point(_hostSurface.ActualWidth, _hostSurface.ActualHeight));
            windowClientTopLeftPx = window.PointToScreen(new Point(0, 0));
        }
        catch
        {
            return;
        }

        var x = (int)Math.Round(hostTopLeftPx.X - windowClientTopLeftPx.X);
        var y = (int)Math.Round(hostTopLeftPx.Y - windowClientTopLeftPx.Y);
        var w = (int)Math.Round(hostBottomRightPx.X - hostTopLeftPx.X);
        var h = (int)Math.Round(hostBottomRightPx.Y - hostTopLeftPx.Y);
        if (w <= 0 || h <= 0) return;

        if (Debugger.IsAttached)
        {
            Debug.WriteLine($"[WebView2Browser:{_id}] Bounds calc: host Actual={_hostSurface.ActualWidth:0.0}x{_hostSurface.ActualHeight:0.0}, px=(x:{x}, y:{y}, w:{w}, h:{h}), parentHwnd=0x{_currentParentWindowHandle.ToInt64():X}");
        }
        // Skip if bounds unchanged
        if (x == _lastX && y == _lastY && w == _lastW && h == _lastH) return;
        _lastX = x; _lastY = y; _lastW = w; _lastH = h;
        _controller.Bounds = new System.Drawing.Rectangle(x, y, w, h);
        try { _controller.NotifyParentWindowPositionChanged(); } catch { }
        _roundedCornerManager.ApplyRoundedRegion(w, h);
    }

    public void ForceBoundsRefresh()
    {
        RunOnUi(() =>
        {
            _lastX = _lastY = _lastW = _lastH = -1;
            try { SyncControllerVisibility(); } catch { }
            try { UpdateControllerBounds(); } catch { }
        });
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
                _handlers.Clear();

                _core.NavigationStarting -= Core_NavigationStarting;
                _core.NavigationCompleted -= Core_NavigationCompleted;
                _core.ContextMenuRequested -= Core_ContextMenuRequested;
                if (!_isChildBrowser)
                {
                    _core.DocumentTitleChanged -= Core_DocumentTitleChanged;
                    _core.FaviconChanged -= Core_FaviconChanged;
                }
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

    public void PromoteToFullTab()
    {
        if (!_isChildBrowser) return;
        _isChildBrowser = false;

        if (_core != null)
        {
            try { _core.DocumentTitleChanged += Core_DocumentTitleChanged; } catch { }
            try { _core.FaviconChanged += Core_FaviconChanged; } catch { }

            try
            {
                _title = _core.DocumentTitle;
                _actionContextBrowser.UpdateTabTitle(_id, _title);
            }
            catch { }

            try
            {
                _favicon = _core.FaviconUri;
                _actionContextBrowser.UpdateTabFavicon(_id, _favicon);
            }
            catch { }
        }
    }

    public void PrepareForReparenting(nint newParentWindowHandle)
    {
        if (newParentWindowHandle == IntPtr.Zero) return;
        _forcedParentWindowHandle = newParentWindowHandle;
        RunOnUi(() =>
        {
            try
            {
                if (_controller != null)
                {
                    // Critical: reparent before the child window closes so the controller isn't destroyed.
                    // Do NOT compute bounds here: at this moment we're often still hosted in the child window,
                    // and applying child-window coordinates to the MainWindow parent yields the "centered small" view.
                    _controller.IsVisible = false;
                    try { _controller.ParentWindow = newParentWindowHandle; } catch { }
                    _currentParentWindowHandle = newParentWindowHandle;
                    _roundedCornerManager.SetParentWindowHandle(newParentWindowHandle);
                    _lastX = _lastY = _lastW = _lastH = -1;
                }
            }
            catch { }
        });
    }

    private static string? NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Scheme)) return "https://" + address;
        return address;
    }

    #region Context Menu Handling

    private void Core_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        // Always handle to suppress any default menu
        e.Handled = true;

        // Capture data from event args synchronously; the args object isn't valid after returning to caller
        var linkUrlSnapshot = "";
        string? imageUrl = null;
        try
        {
            var target = e.ContextMenuTarget;
            linkUrlSnapshot = target?.LinkUri ?? "";
            if (target?.Kind == CoreWebView2ContextMenuTargetKind.Image && !string.IsNullOrWhiteSpace(target.SourceUri))
                imageUrl = target.SourceUri;
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // WebView2 may throw if the target is not available at this time; fallback to empty
            linkUrlSnapshot = "";
        }
        catch
        {
            linkUrlSnapshot = "";
        }

        Dispatcher.BeginInvoke(() =>
        {
            var owner = MainWindow.Instance;

            var cursorPos = VisualDpiUtil.GetCursorPositionInDips(owner);
            var offset = VisualDpiUtil.GetDpiAwareOffset(owner, 12, 12); // 12px right and down, scaled for DPI
            var window = new WebContextMenuWindow(owner, cursorPos.X + offset.X, cursorPos.Y + offset.Y);
            var parameters = new ContextMenuParameters(linkUrlSnapshot, imageUrl);
            window.Prepare(parameters);
            window.Show();
            window.Activate(); // Ensure focus so Deactivated fires on outside click

            // Hide menu on losing activation and return focus to the owner window
            window.Deactivated += ContextMenuWindow_Deactivated;

            void ContextMenuWindow_Deactivated(object? s, EventArgs args)
            {
                window.Deactivated -= ContextMenuWindow_Deactivated;
                try // If closed elsewhere this will throw
                {
                    window.Close();
                }
                catch
                { }
            }
        });
    }

    #endregion
}
