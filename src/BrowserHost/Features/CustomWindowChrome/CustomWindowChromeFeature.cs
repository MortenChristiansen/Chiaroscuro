using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Interop;
using BrowserHost.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features.CustomWindowChrome;

public partial class CustomWindowChromeFeature(MainWindow window) : Feature(window)
{
    private Rect? _lastNormalBounds; // Stored size/position before maximizing (for detach drag only)
    private bool _applyingRestoreBounds; // Prevent recursive capture while programmatically setting during detach

    public override void Configure()
    {
        Window.WindowStyle = WindowStyle.None;
        Window.AllowsTransparency = true;

        Window.ChromeUI.PreviewMouseLeftButtonDown += ChromeUI_PreviewMouseLeftButtonDown;

        Window.ResizeBorder.PreviewMouseMove += ResizeBorder_PreviewMouseMove;
        Window.ResizeBorder.PreviewMouseLeftButtonDown += ResizeBorder_PreviewMouseLeftButtonDown;
        Window.StateChanged += Window_StateChanged;
        Window.LocationChanged += (_, __) => CaptureNormalBounds();
        Window.SizeChanged += (_, __) => CaptureNormalBounds();

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

        CaptureNormalBounds();
    }

    private void CaptureNormalBounds()
    {
        if (_applyingRestoreBounds) return;
        if (Window.WindowState == WindowState.Normal && Window.ActualWidth > 0 && Window.ActualHeight > 0)
        {
            _lastNormalBounds = new Rect(Window.Left, Window.Top, Window.Width, Window.Height);
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        var isMaximized = Window.WindowState == WindowState.Maximized;
        Window.ChromeUI.UpdateWindowState(isMaximized);

        // When transitioning to maximized ensure we have latest normal bounds (already handled by capture logic, but explicit call is cheap)
        if (isMaximized)
            CaptureNormalBounds();
        // Do NOT manually set size/position here; rely on normal WPF + WM_GETMINMAXINFO for proper maximize.
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
            {
                BeginDetachDragFromMaximized(e);
                return;
            }

            try { Window.DragMove(); } catch { }
        }
    }

    private static bool IsMouseOverTransparentPixel(MouseEventArgs e)
    {
        if (e.OriginalSource is Image source && source.Source is BitmapSource bitmap)
        {
            var pos = e.GetPosition(source);
            int x = (int)(pos.X * bitmap.PixelWidth / source.ActualWidth);
            int y = (int)(pos.Y * bitmap.PixelHeight / source.ActualHeight);
            if (x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight)
            {
                byte[] pixels = new byte[4];
                bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
                return pixels[3] == 0; // alpha
            }
        }
        return false;
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

    #region Maximize / Minimize / Detach Drag

    private void ToggleMaximizedState()
    {
        if (Window.WindowState == WindowState.Maximized)
            Window.WindowState = WindowState.Normal;
        else
            Window.WindowState = WindowState.Maximized;
    }

    private void Minimize() => Window.WindowState = WindowState.Minimized;

    private void BeginDetachDragFromMaximized(MouseEventArgs e)
    {
        var wa = GetCurrentMonitorWorkAreaDip();
        var posInWindow = e.GetPosition(Window);
        double percentX = posInWindow.X / Window.ActualWidth;
        double percentY = posInWindow.Y / Window.ActualHeight;

        var restore = _lastNormalBounds ?? Window.RestoreBounds;
        if (restore.Width < 50 || restore.Height < 50)
        {
            restore = new Rect(wa.Left + wa.Width * 0.1, wa.Top + wa.Height * 0.1, wa.Width * 0.8, wa.Height * 0.8);
        }

        if (MonitorInterop.GetCursorPos(out var pt))
        {
            var src = (HwndSource?)PresentationSource.FromVisual(Window);
            double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            double cursorX = pt.X / scaleX;
            double cursorY = pt.Y / scaleY;

            Window.WindowState = WindowState.Normal; // triggers restore

            double targetWidth = restore.Width;
            double targetHeight = restore.Height;

            double newLeft = cursorX - targetWidth * percentX;
            double newTop = cursorY - targetHeight * percentY;

            newLeft = Math.Clamp(newLeft, wa.Left, wa.Right - targetWidth);
            newTop = Math.Clamp(newTop, wa.Top, wa.Bottom - targetHeight);

            _applyingRestoreBounds = true;
            try
            {
                Window.Left = newLeft;
                Window.Top = newTop;
                Window.Width = targetWidth;
                Window.Height = targetHeight;
            }
            finally { _applyingRestoreBounds = false; }

            CaptureNormalBounds();
            Window.UpdateLayout();
            try { Window.DragMove(); } catch { }
        }
    }

    private Rect GetCurrentMonitorWorkAreaDip()
    {
        var hwnd = new WindowInteropHelper(Window).Handle;
        if (hwnd == nint.Zero)
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        var hMon = MonitorInterop.MonitorFromWindow(hwnd, MonitorInterop.MONITOR_DEFAULTTONEAREST);
        var mi = new MonitorInterop.MONITORINFO() { cbSize = Marshal.SizeOf<MonitorInterop.MONITORINFO>() };
        if (!MonitorInterop.GetMonitorInfo(hMon, ref mi))
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        var src = (HwndSource?)PresentationSource.FromVisual(Window);
        double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        double left = mi.rcWork.Left / scaleX;
        double top = mi.rcWork.Top / scaleY;
        double width = (mi.rcWork.Right - mi.rcWork.Left) / scaleX;
        double height = (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY;
        return new Rect(left, top, width, height);
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
        else
        {
            Window.Cursor = Cursors.Arrow;
        }
    }

    private void ResizeBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Window.WindowState == WindowState.Normal && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(Window.ResizeBorder);
            var hit = GetResizeDirection(pos, Window.ResizeBorder.ActualWidth, Window.ResizeBorder.ActualHeight);
            if (hit != HitTest.HTNOWHERE)
                ResizeWindow(hit);
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

    private static Cursor GetCursorForResizeDirection(HitTest hit) => hit switch
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

    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private void ResizeWindow(HitTest hit)
    {
        var hwnd = new WindowInteropHelper(Window).Handle;
        WindowInterop.SendMessage(hwnd, WM_NCLBUTTONDOWN, (nint)hit, nint.Zero);
    }

    #endregion
}
