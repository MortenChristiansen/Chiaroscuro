using BrowserHost.Interop;
using BrowserHost.Tab;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.ActionContext.Tabs;

public class SimpleBrowserWindow : Window
{
    private readonly TabBrowser _browser;
    private Window? _ownerWindow;
    private FrameworkElement? _targetElement; // MainWindow.WebContentBorder
    private const int _cornerRadiusDip = 8;
    private const int _overlayFadeDuration = 300;
    private readonly Border _contentHost;
    private readonly SolidColorBrush _overlayBrush;
    private readonly Grid _rootGrid;
    private bool _isClosing;
    private bool _contentAnimationStarted;

    public SimpleBrowserWindow(string address)
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;

        _browser = new TabBrowser($"{Guid.NewGuid()}", address, MainWindow.Instance.ActionContext, setManualAddress: false, favicon: null, isChildBrowser: true);
        _browser.PageLoadEnded += Browser_PageLoadEnded;

        // Root overlay with semi-transparent outer area (animate opacity on load)
        _overlayBrush = new SolidColorBrush(Color.FromArgb(128, 180, 180, 200)) { Opacity = 0.0 };
        _rootGrid = new Grid { Background = _overlayBrush };

        // Centered content host – we size it relative to window size
        _contentHost = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(8),
            SnapsToDevicePixels = true,
            Child = _browser,
            Opacity = 0,
            LayoutTransform = new ScaleTransform(0.5, 0.5)
        };

        _rootGrid.Children.Add(_contentHost);
        Content = _rootGrid;

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
        _rootGrid.PreviewMouseDown += RootGrid_PreviewMouseDown;
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

    private void Browser_PageLoadEnded(object? sender, EventArgs e)
    {
        // Run on UI thread and only once
        Dispatcher.BeginInvoke(() =>
        {
            if (_contentAnimationStarted) return;
            _contentAnimationStarted = true;
            AnimateContentIn();
            // Unsubscribe after first main frame load
            try { _browser.PageLoadEnded -= Browser_PageLoadEnded; } catch { }
        });
    }

    private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Close only when clicking outside the centered content host
        var pos = e.GetPosition(_contentHost);
        if (pos.X < 0 || pos.Y < 0 || pos.X > _contentHost.ActualWidth || pos.Y > _contentHost.ActualHeight)
        {
            e.Handled = true;
            BeginCloseWithFade();
            AnimateContentOut();
        }
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
        try { _browser.PageLoadEnded -= Browser_PageLoadEnded; } catch { }
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
            int r = (int)Math.Round(_cornerRadiusDip * (scaleX + scaleY) / 2.0);
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
            Duration = TimeSpan.FromMilliseconds(_overlayFadeDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _overlayBrush.BeginAnimation(Brush.OpacityProperty, fade);
    }

    private void AnimateContentIn()
    {
        // Ensure we have a ScaleTransform as LayoutTransform
        if (_contentHost.LayoutTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform(0.5, 0.5);
            _contentHost.LayoutTransform = scale;
        }

        // Opacity: 0 -> 1
        var opacityAnim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _contentHost.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

        // Scale: 0.5 -> 1.0 (both axes)
        var scaleAnim = new DoubleAnimation
        {
            From = 0.5,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    private void AnimateContentOut()
    {
        // Ensure we have a ScaleTransform as LayoutTransform
        var scale = new ScaleTransform(1.0, 1.0);
        _contentHost.LayoutTransform = scale;
        // Opacity: 1 -> 0
        var opacityAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        _contentHost.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        // Scale: 1.0 -> 0.5 (both axes)
        var scaleAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.5,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
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

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            BeginCloseWithFade();
            AnimateContentOut();
            return;
        }
        base.OnClosing(e);
    }

    private void BeginCloseWithFade()
    {
        if (_isClosing) return;
        _isClosing = true;

        var fade = new DoubleAnimation
        {
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(_overlayFadeDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, __) =>
        {
            // Stop animation and close
            _overlayBrush.BeginAnimation(Brush.OpacityProperty, null);
            try { base.Close(); } catch { }
        };
        _overlayBrush.BeginAnimation(Brush.OpacityProperty, fade);
    }
}
