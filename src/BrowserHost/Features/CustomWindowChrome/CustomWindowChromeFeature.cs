using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features.CustomWindowChrome;

public partial class CustomWindowChromeFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        Window.WindowStyle = WindowStyle.None;
        Window.AllowsTransparency = true;

        Window.ChromeUI.PreviewMouseLeftButtonDown += ChromeUI_PreviewMouseLeftButtonDown;
        Window.PreviewMouseLeftButtonUp += MainWindow_PreviewMouseLeftButtonUp;
        Window.PreviewMouseMove += MainWindow_PreviewMouseMove;

        Window.ResizeBorder.PreviewMouseMove += ResizeBorder_PreviewMouseMove;
        Window.ResizeBorder.PreviewMouseLeftButtonDown += ResizeBorder_PreviewMouseLeftButtonDown;

        // Prevent maximizing over the taskbar
        Window.StateChanged += (s, e) => AdjustWindowBorder();
        // Add a lightweight post state-change animation when maximize/restore happens outside our own handlers
        Window.StateChanged += Window_StateChangedAnimate;

        PubSub.Subscribe<WindowMinimizedEvent>(_ => Minimize());
        PubSub.Subscribe<WindowStateToggledEvent>(_ => ToggleMaximizedState());
        PubSub.Subscribe<AddressCopyRequestedEvent>(_ =>
        {
            var address = Window.CurrentTab?.Address;
            if (!string.IsNullOrEmpty(address))
                Clipboard.SetText(address);
        });
        PubSub.Subscribe<TabLoadingStateChangedEvent>(OnTabLoadingStateChanged);
        PubSub.Subscribe<TabActivatedEvent>(OnTabActivated);

        // Track initial state for correct position restore logic
        _previousState = Window.WindowState;
    }

    private void ChromeUI_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && IsMouseOverTransparentPixel(e))
        {
            ToggleMaximizedState();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed && IsMouseOverTransparentPixel(e))
        {
            if (Window.WindowState == WindowState.Maximized)
                HandleDragToDetachFromMaximizedState(e);

            Window.DragMove();
        }
    }

    private static bool IsMouseOverTransparentPixel(MouseEventArgs e)
    {
        if (e.OriginalSource is Image source && source.Source is BitmapSource bitmap)
        {
            // Get mouse position relative to the image
            var pos = e.GetPosition(source);
            int x = (int)(pos.X * bitmap.PixelWidth / source.ActualWidth);
            int y = (int)(pos.Y * bitmap.PixelHeight / source.ActualHeight);
            if (x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight)
            {
                byte[] pixels = new byte[4];
                bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
                byte alpha = pixels[3];
                return alpha == 0;
            }
        }
        return false;
    }

    private void MainWindow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Released && _isDraggingToDetach)
            ResetDetachDrag();
    }

    private void MainWindow_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _isDraggingToDetach)
            HandleDragToDetachFromMaximizedState(e);
    }

    private void OnTabLoadingStateChanged(TabLoadingStateChangedEvent e)
    {
        if (Window.CurrentTab?.Id == e.TabId)
            Window.ChromeUI.UpdateLoadingState(e.IsLoading);
    }

    private void OnTabActivated(TabActivatedEvent e)
    {
        var isLoading = e.PreviousTab?.IsLoading ?? false;
        Window.ChromeUI.UpdateLoadingState(isLoading);
    }

    #region Minimize/Maximize

    private void ToggleMaximizedState()
    {
        // Let the animation method handle toggling the state
        _ = AnimateToggleMaximizeAsync();
    }

    private void Minimize()
    {
        _ = AnimateMinimizeAsync();
    }

    private async Task AnimateToggleMaximizeAsync()
    {
        if (_isAnimating) return;
        var target = Window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        _suppressNextStateChangeAnimation = true;
        Window.WindowState = target;

        // Subtle bounce/settle animation post state change
        var root = GetRoot();
        if (root is null) return;
        EnsureTransforms(root);

        var initialScale = target == WindowState.Maximized ? 0.985 : 1.015;
        _scaleTransform.ScaleX = initialScale;
        _scaleTransform.ScaleY = initialScale;
        root.Opacity = 0.985;

        _isAnimating = true;
        try
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(140);

            root.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });

            await Task.Delay(duration + TimeSpan.FromMilliseconds(20));
        }
        finally
        {
            // Clear animations and reset to identity
            root.BeginAnimation(UIElement.OpacityProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            root.Opacity = 1.0;
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;
            _isAnimating = false;
        }
    }

    private async Task AnimateMinimizeAsync()
    {
        if (_isAnimating) return;
        var root = GetRoot();
        if (root is null)
        {
            Window.WindowState = WindowState.Minimized;
            return;
        }

        EnsureTransforms(root);
        _isAnimating = true;
        try
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
            var duration = TimeSpan.FromMilliseconds(120);

            root.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, duration) { EasingFunction = ease });
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.96, duration) { EasingFunction = ease });
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.96, duration) { EasingFunction = ease });

            await Task.Delay(duration + TimeSpan.FromMilliseconds(20));
        }
        finally
        {
            Window.WindowState = WindowState.Minimized;
            // Clear animations and reset for when restored
            if (root is not null)
            {
                root.BeginAnimation(UIElement.OpacityProperty, null);
                _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                root.Opacity = 1.0;
                _scaleTransform.ScaleX = 1.0;
                _scaleTransform.ScaleY = 1.0;
            }
            _isAnimating = false;
        }
    }

    private void Window_StateChangedAnimate(object? sender, EventArgs e)
    {
        if (_suppressNextStateChangeAnimation)
        {
            _suppressNextStateChangeAnimation = false;
            return;
        }

        // Only add a subtle settle animation for maximize/restore changes initiated externally
        if (Window.WindowState is WindowState.Maximized or WindowState.Normal)
        {
            var root = GetRoot();
            if (root is null) return;
            EnsureTransforms(root);

            var initial = Window.WindowState == WindowState.Maximized ? 0.985 : 1.015;
            _ = AnimateBounceAsync(root, initial);
        }
    }

    private async Task AnimateBounceAsync(FrameworkElement root, double initialScale)
    {
        if (_isAnimating) return;
        EnsureTransforms(root);
        _scaleTransform.ScaleX = initialScale;
        _scaleTransform.ScaleY = initialScale;
        root.Opacity = 0.985;

        _isAnimating = true;
        try
        {
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = TimeSpan.FromMilliseconds(140);

            root.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(1.0, duration) { EasingFunction = ease });

            await Task.Delay(duration + TimeSpan.FromMilliseconds(20));
        }
        finally
        {
            root.BeginAnimation(UIElement.OpacityProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            root.Opacity = 1.0;
            _scaleTransform.ScaleX = 1.0;
            _scaleTransform.ScaleY = 1.0;
            _isAnimating = false;
        }
    }

    private FrameworkElement? GetRoot()
    {
        return Window.Content as FrameworkElement;
    }

    private void EnsureTransforms(FrameworkElement root)
    {
        if (root.RenderTransform is not TransformGroup group)
        {
            group = new TransformGroup();
            _scaleTransform = new ScaleTransform(1.0, 1.0);
            group.Children.Add(_scaleTransform);
            root.RenderTransform = group;
        }
        else
        {
            // Try find existing scale transform
            ScaleTransform? st = null;
            foreach (var t in group.Children)
            {
                if (t is ScaleTransform s)
                {
                    st = s;
                    break;
                }
            }
            _scaleTransform = st ?? new ScaleTransform(1.0, 1.0);
            if (!group.Children.Contains(_scaleTransform))
                group.Children.Insert(0, _scaleTransform);
        }

        root.RenderTransformOrigin = new Point(0.5, 0.5);
    }

    private ScaleTransform _scaleTransform = new(1.0, 1.0);
    private bool _isAnimating;
    private bool _suppressNextStateChangeAnimation;

    private double _lastX = 0;
    private double _lastY = 0;
    private WindowState _previousState;

    private void AdjustWindowBorder()
    {
        if (Window.WindowState == WindowState.Maximized)
        {
            // Only capture restore position when entering maximized state
            if (_previousState != WindowState.Maximized)
            {
                _lastX = Window.Left;
                _lastY = Window.Top;
            }

            // Use the current monitor's work area (in DIPs) so we never overlap the taskbar
            var wa = GetCurrentMonitorWorkAreaDip();

            Window.MaxWidth = wa.Width;
            Window.MaxHeight = wa.Height;
            Window.Left = wa.Left;
            Window.Top = wa.Top;
        }
        else if (Window.WindowState == WindowState.Normal)
        {
            Window.MaxWidth = double.PositiveInfinity;
            Window.MaxHeight = double.PositiveInfinity;

            // Only restore position if we just came from maximized
            if (_previousState == WindowState.Maximized)
            {
                Window.Left = _lastX;
                Window.Top = _lastY;
            }
        }

        _previousState = Window.WindowState;
    }

    private Rect GetCurrentMonitorWorkAreaDip()
    {
        var hwnd = new WindowInteropHelper(Window).Handle;
        if (hwnd == nint.Zero)
        {
            // Fallback to primary work area
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO();
        mi.cbSize = Marshal.SizeOf<MONITORINFO>();
        if (!GetMonitorInfo(hMon, ref mi))
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        // Convert from physical pixels to device independent units
        var src = (HwndSource?)PresentationSource.FromVisual(Window);
        double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        double left = mi.rcWork.Left / scaleX;
        double top = mi.rcWork.Top / scaleY;
        double width = (mi.rcWork.Right - mi.rcWork.Left) / scaleX;
        double height = (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY;
        return new Rect(left, top, width, height);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [LibraryImport("user32.dll")]
    private static partial nint MonitorFromWindow(nint hwnd, uint dwFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(nint hMonitor, ref MONITORINFO lpmi);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    #endregion

    #region Window Detach Drag Handling

    private bool _isDraggingToDetach = false;
    private Point? _dragStartPoint;
    private void HandleDragToDetachFromMaximizedState(MouseEventArgs e)
    {
        if (_isDraggingToDetach && _dragStartPoint.HasValue)
        {
            // If already dragging, calculate the distance moved
            var currentPoint = e.GetPosition(Window);
            var distance = (currentPoint - _dragStartPoint.Value).Length;
            // If the mouse has moved significantly, allow detaching
            if (distance < 20)
                return; // Not enough movement to detach
        }

        if (!_isDraggingToDetach)
        {
            StartDetachDrag(e);
            return;
        }

        PerformDetachOfWindow(e);

        e.Handled = true;
        ResetDetachDrag();
        Window.ChromeUI.ReleaseMouseCapture();
        Window.DragMove();
    }

    private void PerformDetachOfWindow(MouseEventArgs e)
    {
        // Calculate mouse position relative to window
        var mouseX = e.GetPosition(Window).X;
        var percentX = mouseX / Window.ActualWidth;
        var mouseY = e.GetPosition(Window).Y;
        var percentY = mouseY / Window.ActualHeight;

        // Set to normal state
        Window.WindowState = WindowState.Normal;

        // Get cursor in screen pixels, convert to DIPs, then clamp to current monitor work area
        if (GetCursorPos(out var pt))
        {
            var src = (HwndSource?)PresentationSource.FromVisual(Window);
            var m = src?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var cursorDip = m.Transform(new Point(pt.X, pt.Y));

            var wa = GetCurrentMonitorWorkAreaDip();
            var newLeft = cursorDip.X - Window.ActualWidth * percentX;
            var newTop = cursorDip.Y - Window.ActualHeight * percentY;

            newLeft = Math.Clamp(newLeft, wa.Left, wa.Right - Window.ActualWidth);
            newTop = Math.Clamp(newTop, wa.Top, wa.Bottom - Window.ActualHeight);

            Window.Left = newLeft;
            Window.Top = newTop;
        }

        Window.UpdateLayout();
    }

    private void StartDetachDrag(MouseEventArgs e)
    {
        _dragStartPoint = e.GetPosition(Window);
        _isDraggingToDetach = true;
        Window.Cursor = Cursors.SizeAll;
        // Prevents selecting elements as we complete the detach operation
        Window.ChromeUI.EvaluateScriptAsync("document.body.setAttribute('inert', '');").GetAwaiter().GetResult();
    }

    private void ResetDetachDrag()
    {
        _dragStartPoint = null;
        _isDraggingToDetach = false;
        Window.Cursor = Cursors.Arrow;
        Window.ChromeUI.EvaluateScriptAsync("document.body.removeAttribute('inert');").GetAwaiter().GetResult();
    }

    #endregion

    #region Resize Border Handling

    private void ResizeBorder_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (Window.WindowState == WindowState.Normal)
        {
            var pos = e.GetPosition(Window.ResizeBorder);
            var hit = GetResizeDirection(pos, Window.ResizeBorder.ActualWidth, Window.ResizeBorder.ActualHeight);
            Window.Cursor = GetCursorForResizeDirection(hit);
        }
    }

    private void ResizeBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window.WindowState == WindowState.Normal && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(Window.ResizeBorder);
            var hit = GetResizeDirection(pos, Window.ResizeBorder.ActualWidth, Window.ResizeBorder.ActualHeight);
            if (hit != HitTest.HTNOWHERE)
            {
                ResizeWindow(hit);
            }
        }
    }

    private enum HitTest
    {
        HTNOWHERE = 0,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17
    }

    private static HitTest GetResizeDirection(Point pos, double width, double height)
    {
        const int edge = 8;
        bool left = pos.X >= 0 && pos.X < edge;
        bool right = pos.X <= width && pos.X > width - edge;
        bool top = pos.Y >= 0 && pos.Y < edge;
        bool bottom = pos.Y <= height && pos.Y > height - edge;

        if (left && top) return HitTest.HTTOPLEFT;
        if (right && top) return HitTest.HTTOPRIGHT;
        if (left && bottom) return HitTest.HTBOTTOMLEFT;
        if (right && bottom) return HitTest.HTBOTTOMRIGHT;
        if (left) return HitTest.HTLEFT;
        if (right) return HitTest.HTRIGHT;
        if (top) return HitTest.HTTOP;
        if (bottom) return HitTest.HTBOTTOM;
        return HitTest.HTNOWHERE;
    }

    private static Cursor GetCursorForResizeDirection(HitTest hit) =>
        hit switch
        {
            HitTest.HTLEFT => Cursors.SizeWE,
            HitTest.HTRIGHT => Cursors.SizeWE,
            HitTest.HTTOP => Cursors.SizeNS,
            HitTest.HTBOTTOM => Cursors.SizeNS,
            HitTest.HTTOPLEFT => Cursors.SizeNWSE,
            HitTest.HTTOPRIGHT => Cursors.SizeNESW,
            HitTest.HTBOTTOMLEFT => Cursors.SizeNESW,
            HitTest.HTBOTTOMRIGHT => Cursors.SizeNWSE,
            _ => Cursors.Arrow
        };

    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);
    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private void ResizeWindow(HitTest hit)
    {
        var hwnd = new WindowInteropHelper(Window).Handle;
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (nint)hit, nint.Zero);
    }

    #endregion
}
