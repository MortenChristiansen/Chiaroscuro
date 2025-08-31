using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.ActionDialog;
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
using System.Windows.Media.Imaging;

namespace BrowserHost.Tab.WebView2;

public sealed class WebView2Browser : UserControl, ITabWebBrowser, IDisposable
{
    private static CoreWebView2Environment? _sharedEnvironment;
    private readonly string _id;
    private readonly string? _initialManualAddress;
    private readonly string? _initialFavicon;
    private string? _manualAddress;
    private string? _favicon;
    private string _title = string.Empty;
    private bool _isLoading;
    private CoreWebView2Controller? _controller;
    private CoreWebView2? _core;
    private readonly Border _hostSurface = new() { Background = Brushes.Transparent };
    private string? _pendingNavigateTo;
    private string? _lastAddressSnapshot;
    private Image? _snapshotImage;
    private bool _snapshotActive;
    private double _zoomFactor = 1.0; // track requested zoom

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
        _hostSurface.Loaded += async (_, _) => { await EnsureControllerAsync(actionContextBrowser); SyncControllerVisibility(); };
        _hostSurface.Unloaded += (_, _) => SyncControllerVisibility();
        _hostSurface.IsVisibleChanged += (_, _) => SyncControllerVisibility();
        _hostSurface.SizeChanged += (_, _) => { UpdateControllerBounds(); };
        _hostSurface.LayoutUpdated += (_, _) => { if (_hostSurface.IsVisible) UpdateControllerBounds(); };

        // Subscribe to Action Dialog lifecycle to toggle snapshot overlay
        PubSub.Subscribe<ActionDialogShownEvent>(HandleActionDialogShownEvent);
        PubSub.Subscribe<ActionDialogDismissedEvent>(HandleActionDialogDismissedEvent);
    }

    public string Id => _id;
    public string? Favicon => _favicon ?? _initialFavicon;
    public string? ManualAddress => _manualAddress ?? _initialManualAddress;
    public string Address => _core?.Source ?? _pendingNavigateTo ?? string.Empty;
    public string Title { get => _title; set => _title = value; }
    public bool IsLoading => _isLoading;
    public bool CanGoBack => _core?.CanGoBack ?? false;
    public bool CanGoForward => _core?.CanGoForward ?? false;
    public bool HasDevTools => false;
    public double DefaultZoomLevel => 1.0;

    private void HandleActionDialogShownEvent(ActionDialogShownEvent _)
    {
        if (_hostSurface.IsVisible)
            ActivateSnapshotAsync();
    }

    private void HandleActionDialogDismissedEvent(ActionDialogDismissedEvent _)
    {
        if (_snapshotActive)
            DeactivateSnapshot();
    }

    private async Task EnsureControllerAsync(ActionContextBrowser actionContextBrowser)
    {
        if (_controller != null) return;
        if (_sharedEnvironment == null)
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WebView2LowLevelCache");
            Directory.CreateDirectory(userDataFolder);
            var options = new CoreWebView2EnvironmentOptions
            {
                AllowSingleSignOnUsingOSPrimaryAccount = true,
            };
            _sharedEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder, options: options);
        }
        var parentWindow = Window.GetWindow(_hostSurface);
        if (parentWindow == null) return;
        var hwnd = new WindowInteropHelper(parentWindow).Handle;
        _controller = await _sharedEnvironment.CreateCoreWebView2ControllerAsync(hwnd);
        _core = _controller.CoreWebView2;
        UpdateControllerBounds();
        WireCoreEvents(actionContextBrowser);
        ApplySettings();
        if (_pendingNavigateTo != null) { _core.Navigate(_pendingNavigateTo); _pendingNavigateTo = null; }
    }

    private void WireCoreEvents(ActionContextBrowser actionContextBrowser)
    {
        if (_core == null) return;
        _core.NavigationStarting += (_, __) => _isLoading = true;
        _core.NavigationCompleted += (_, __) =>
        {
            _isLoading = false;
            var newAddress = _core.Source;
            if (_lastAddressSnapshot != newAddress)
            {
                var previous = _lastAddressSnapshot;
                _lastAddressSnapshot = newAddress;

                AddressChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(AddressProperty, previous, newAddress));
            }
            // Invalidate snapshot after navigation (so next dialog show captures fresh view)
            if (_snapshotActive) RefreshSnapshotAsync();
        };
        _core.DocumentTitleChanged += (_, __) => { _title = _core.DocumentTitle; actionContextBrowser.UpdateTabTitle(_id, _title); };
        _core.FaviconChanged += (_, __) => { _favicon = _core.FaviconUri; actionContextBrowser.UpdateTabFavicon(_id, _favicon); };
        _controller!.AcceleratorKeyPressed += Controller_AcceleratorKeyPressed;
    }

    private void ApplySettings()
    {
        if (_core == null) return;

        var settings = _core.Settings;
        settings.AreDevToolsEnabled = true;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = true; // we consume them
        // Apply zoom on controller if available
        if (_controller != null)
        {
            try { _controller.ZoomFactor = _zoomFactor; } catch { }
        }
    }

    private static readonly HashSet<Key> _allowedCtrlEditingKeys = [Key.C, Key.V, Key.X, Key.A, Key.Z, Key.Y];
    private void Controller_AcceleratorKeyPressed(object? sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
    {
        if (e.KeyEventKind is not (CoreWebView2KeyEventKind.KeyDown or CoreWebView2KeyEventKind.SystemKeyDown)) return;

        var key = KeyInterop.KeyFromVirtualKey((int)e.VirtualKey);
        var mods = Keyboard.Modifiers;
        var ctrl = (mods & ModifierKeys.Control) != 0;
        var alt = (mods & ModifierKeys.Alt) != 0;
        var forward = false;

        if (alt)
        {
            forward = true;
        }
        else if (key is Key.F5 or Key.F12)
        {
            forward = true;
        }
        else if (ctrl && !_allowedCtrlEditingKeys.Contains(key))
        {
            forward = true;
        }
        else if (ctrl && key == Key.Tab)
        {
            forward = true;
        }

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

        var shouldBeVisible = _hostSurface.IsVisible && !_snapshotActive;

        if (_controller.IsVisible != shouldBeVisible)
            _controller.IsVisible = shouldBeVisible;

        if (shouldBeVisible)
            UpdateControllerBounds();
    }

    private void UpdateControllerBounds()
    {
        if (_controller == null || _snapshotActive || !_hostSurface.IsVisible) return;
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
        _controller.Bounds = new System.Drawing.Rectangle(x, y, w, h);
    }

    private async void ActivateSnapshotAsync()
    {
        if (_snapshotActive || _core == null || _controller == null || !_hostSurface.IsVisible) return;

        try
        {
            using var ms = new MemoryStream();
            await _core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            ms.Position = 0;
            var bmp = new BitmapImage();
            bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.StreamSource = ms; bmp.EndInit(); bmp.Freeze();
            _snapshotImage = new Image { Source = bmp, Stretch = Stretch.Fill, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            _hostSurface.Child = _snapshotImage;
            _controller.IsVisible = false;
            _snapshotActive = true;
        }
        catch { /* ignore */ }
    }

    private async void RefreshSnapshotAsync()
    {
        if (!_snapshotActive || _core == null) return;
        try
        {
            using var ms = new MemoryStream();
            await _core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            ms.Position = 0;
            var bmp = new BitmapImage();
            bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.StreamSource = ms; bmp.EndInit(); bmp.Freeze();
            if (_snapshotImage != null) _snapshotImage.Source = bmp;
        }
        catch { }
    }

    private void DeactivateSnapshot()
    {
        if (!_snapshotActive) return;
        _hostSurface.Child = null;
        _snapshotImage = null;
        _snapshotActive = false;
        SyncControllerVisibility();
    }

    public void SetAddress(string address, bool setManualAddress)
    {
        var normalized = NormalizeAddress(address);
        if (setManualAddress) _manualAddress = address;
        if (_core == null) { _pendingNavigateTo = normalized; return; }
        if (normalized != null) _core.Navigate(normalized);
    }

    public void RegisterContentPageApi(BrowserApi api, string name) => _core?.AddHostObjectToScript(name, api);
    public void Reload(bool ignoreCache = false) => _core?.Reload();
    public void Back() { if (CanGoBack) _core?.GoBack(); }
    public void Forward() { if (CanGoForward) _core?.GoForward(); }
    public async Task CallClientApi(string api, string? arguments = null) { if (_core != null) await _core.ExecuteScriptAsync($"{api}({arguments ?? string.Empty});"); }
    public Task<double> GetZoomLevelAsync() => Task.FromResult(_controller?.ZoomFactor ?? _zoomFactor);

    public void SetZoomLevel(double level)
    {
        var clamped = Math.Clamp(level, 0.25, 5.0); // WebView2 supported range
        _zoomFactor = clamped;
        if (_controller != null)
        {
            try { _controller.ZoomFactor = clamped; } catch { }
        }
    }
    public void Find(string searchText, bool forward, bool matchCase, bool findNext) { }
    public void StopFinding(bool clearSelection) { }
    public UIElement AsUIElement() => _hostSurface;
    public void ShowDevTools() => _core?.OpenDevToolsWindow();
    public void CloseDevTools() { }

    public void Dispose()
    {
        try
        {
            // Unsubscribe from PubSub events
            PubSub.Unsubscribe<ActionDialogShownEvent>(HandleActionDialogShownEvent);
            PubSub.Unsubscribe<ActionDialogDismissedEvent>(HandleActionDialogDismissedEvent);
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
