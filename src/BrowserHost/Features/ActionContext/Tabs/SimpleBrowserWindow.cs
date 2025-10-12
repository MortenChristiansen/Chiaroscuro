using BrowserHost.Interop;
using CefSharp.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.ActionContext.Tabs;

public class SimpleBrowserWindow : Window
{
    private readonly ChromiumWebBrowser _browser;
    private Window? _ownerWindow;
    private FrameworkElement? _targetElement; // MainWindow.WebContentBorder
    private const int CornerRadiusDip = 8;
    private readonly Border _contentHost;
    private readonly SolidColorBrush _overlayBrush;

    public SimpleBrowserWindow(string address)
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;

        _browser = new ChromiumWebBrowser { Address = address };

        // Root overlay with semi-transparent outer area (animate opacity on load)
        _overlayBrush = new SolidColorBrush(Color.FromArgb(128, 180, 180, 200)) { Opacity = 0.0 };
        var root = new Grid { Background = _overlayBrush };

        // Centered content host – we size it relative to window size
        _contentHost = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(8),
            SnapsToDevicePixels = true,
            Child = _browser
        };

        root.Children.Add(_contentHost);
        Content = root;

        Loaded += OnLoadedAttachToOwner;
        Loaded += (_, __) =>
        {
            TryHookDpiAndApplyRoundedCorners();
            ApplyRoundedWindowRegion();
        };
        Closed += (_, __) => DetachOwnerHandlers();
        SizeChanged += (_, __) => ApplyRoundedWindowRegion();
        StateChanged += (_, __) => ApplyRoundedWindowRegion();
        SizeChanged += (_, __) => UpdateContentHostSize();
    }

    private void OnLoadedAttachToOwner(object? sender, RoutedEventArgs e)
    {
        _ownerWindow = Owner ?? Window.GetWindow(MainWindow.Instance);
        if (_ownerWindow == null) return;

        _targetElement = MainWindow.Instance.WebContentBorder;
        UpdateOverlayBounds();
        _contentHost.SizeChanged += (_, __) => UpdateContentCornerClip();
        UpdateContentHostSize();

        _ownerWindow.LocationChanged += OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.SizeChanged += OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.StateChanged += OwnerWindow_StateChanged;
        if (_targetElement != null)
        {
            _targetElement.SizeChanged += TargetElement_SizeOrLayoutChanged;
            _targetElement.LayoutUpdated += TargetElement_SizeOrLayoutChanged;
        }

        // Start overlay fade-in once sized and positioned
        AnimateOverlayIn();
        UpdateContentCornerClip();
    }

    private void OwnerWindow_StateChanged(object? sender, EventArgs e)
    {
        if (_ownerWindow == null) return;
        if (_ownerWindow.WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Minimized;
        }
        else if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        OwnerWindow_LocationOrSizeChanged(sender!, e);
    }

    private void OwnerWindow_LocationOrSizeChanged(object? sender, EventArgs e)
    {
        if (_ownerWindow == null) return;
        UpdateOverlayBounds();
    }

    private void TargetElement_SizeOrLayoutChanged(object? sender, EventArgs e) => UpdateOverlayBounds();

    private void UpdateOverlayBounds()
    {
        if (_ownerWindow == null || _targetElement == null) return;
        if (!IsLoaded) return;

        // Get position of WebContentBorder relative to owner window (DIPs)
        var tl = _targetElement.TranslatePoint(new Point(0, 0), _ownerWindow);
        var w = _targetElement.ActualWidth;
        var h = _targetElement.ActualHeight;
        if (w <= 0 || h <= 0) return;

        Left = _ownerWindow.Left + tl.X;
        Top = _ownerWindow.Top + tl.Y;
        Width = w;
        Height = h;
    }

    private void DetachOwnerHandlers()
    {
        if (_targetElement != null)
        {
            _targetElement.SizeChanged -= TargetElement_SizeOrLayoutChanged;
            _targetElement.LayoutUpdated -= TargetElement_SizeOrLayoutChanged;
            _targetElement = null;
        }
        if (_ownerWindow != null)
        {
            _ownerWindow.LocationChanged -= OwnerWindow_LocationOrSizeChanged;
            _ownerWindow.SizeChanged -= OwnerWindow_LocationOrSizeChanged;
            _ownerWindow.StateChanged -= OwnerWindow_StateChanged;
            _ownerWindow = null;
        }
    }

    // Hook into DPI changes and apply rounded corner window region
    private void TryHookDpiAndApplyRoundedCorners()
    {
        var src = (HwndSource?)PresentationSource.FromVisual(this);
        src?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_DPICHANGED = 0x02E0;
        if (msg == WM_DPICHANGED)
        {
            Dispatcher.BeginInvoke(ApplyRoundedWindowRegion);
        }
        return IntPtr.Zero;
    }

    private void ApplyRoundedWindowRegion()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var src = (HwndSource?)PresentationSource.FromVisual(this);
            double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            int w = (int)Math.Max(1, Math.Round(ActualWidth * scaleX));
            int h = (int)Math.Max(1, Math.Round(ActualHeight * scaleY));
            int r = (int)Math.Round(CornerRadiusDip * (scaleX + scaleY) / 2.0);
            r = Math.Max(1, r);

            nint rgn = WindowInterop.CreateRoundRectRgn(0, 0, w + 1, h + 1, r * 2, r * 2);
            if (rgn != nint.Zero)
            {
                var ok = WindowInterop.SetWindowRgn(hwnd, rgn, true);
                if (ok == 0)
                {
                    WindowInterop.DeleteObject(rgn);
                }
            }
        }
        catch { }
    }

    private void UpdateContentHostSize()
    {
        // Scale down inner browser to, e.g., 85% of container size, keep minimums
        var targetW = Math.Max(400, ActualWidth * 0.85);
        var targetH = Math.Max(300, ActualHeight * 0.85);
        _contentHost.Width = targetW;
        _contentHost.Height = targetH;
    }

    private void AnimateOverlayIn()
    {
        var fade = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _overlayBrush.BeginAnimation(Brush.OpacityProperty, fade);
    }

    private void UpdateContentCornerClip()
    {
        var w = _contentHost.ActualWidth;
        var h = _contentHost.ActualHeight;
        if (w <= 0 || h <= 0)
        {
            _contentHost.Clip = null;
            return;
        }
        var radius = 8.0; // match CornerRadius
        _contentHost.Clip = new RectangleGeometry(new Rect(0, 0, w, h), radius, radius);
    }
}
