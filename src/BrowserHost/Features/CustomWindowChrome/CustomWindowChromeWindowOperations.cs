using BrowserHost.Interop;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeWindowOperations
{
    private MainWindow _window = null!;
    private bool _integrated;
    private CustomWindowChromeBrowserApi? _customWindowChromeApi;
    private Rect? _lastNormalBounds;
    private bool _applyingRestoreBounds;

    protected CustomWindowChromeWindowOperations() { }

    public CustomWindowChromeWindowOperations(MainWindow window)
    {
        _window = window;
    }

    public virtual void EnableCustomWindowChrome(CustomWindowChromeBrowserApi customWindowChromeApi)
    {
        if (_integrated)
            return;

        _integrated = true;
        _customWindowChromeApi = customWindowChromeApi;

        _window.WindowStyle = WindowStyle.None;
        _window.AllowsTransparency = true;

        _window.ChromeUI.PreviewMouseLeftButtonDown += ChromeUI_PreviewMouseLeftButtonDown;

        _window.ResizeBorder.PreviewMouseMove += ResizeBorder_PreviewMouseMove;
        _window.ResizeBorder.PreviewMouseLeftButtonDown += ResizeBorder_PreviewMouseLeftButtonDown;
        _window.StateChanged += Window_StateChanged;
        _window.LocationChanged += (_, __) => CaptureNormalBounds();
        _window.SizeChanged += (_, __) => CaptureNormalBounds();

        CaptureNormalBounds();
    }

    private void CaptureNormalBounds()
    {
        if (_applyingRestoreBounds) return;
        if (_window.WindowState == WindowState.Normal && _window.ActualWidth > 0 && _window.ActualHeight > 0)
        {
            _lastNormalBounds = new Rect(_window.Left, _window.Top, _window.Width, _window.Height);
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        var isMaximized = _window.WindowState == WindowState.Maximized;
        _customWindowChromeApi?.UpdateWindowState(isMaximized);

        if (isMaximized)
            CaptureNormalBounds();
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
            if (_window.WindowState == WindowState.Maximized)
            {
                BeginDetachDragFromMaximized(e);
                return;
            }

            try { _window.DragMove(); } catch { }
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

    private void ToggleMaximizedState()
    {
        if (_window.WindowState == WindowState.Maximized)
            _window.WindowState = WindowState.Normal;
        else
            _window.WindowState = WindowState.Maximized;
    }

    private void BeginDetachDragFromMaximized(MouseEventArgs e)
    {
        var wa = GetCurrentMonitorWorkAreaDip();
        var posInWindow = e.GetPosition(_window);
        double percentX = posInWindow.X / _window.ActualWidth;
        double percentY = posInWindow.Y / _window.ActualHeight;

        var restore = _lastNormalBounds ?? _window.RestoreBounds;
        if (restore.Width < 50 || restore.Height < 50)
        {
            restore = new Rect(wa.Left + wa.Width * 0.1, wa.Top + wa.Height * 0.1, wa.Width * 0.8, wa.Height * 0.8);
        }

        if (MonitorInterop.GetCursorPos(out var pt))
        {
            var src = (HwndSource?)PresentationSource.FromVisual(_window);
            double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            double cursorX = pt.X / scaleX;
            double cursorY = pt.Y / scaleY;

            _window.WindowState = WindowState.Normal; // triggers restore

            double targetWidth = restore.Width;
            double targetHeight = restore.Height;

            double newLeft = cursorX - targetWidth * percentX;
            double newTop = cursorY - targetHeight * percentY;

            newLeft = Math.Clamp(newLeft, wa.Left, wa.Right - targetWidth);
            newTop = Math.Clamp(newTop, wa.Top, wa.Bottom - targetHeight);

            _applyingRestoreBounds = true;
            try
            {
                _window.Left = newLeft;
                _window.Top = newTop;
                _window.Width = targetWidth;
                _window.Height = targetHeight;
            }
            finally { _applyingRestoreBounds = false; }

            CaptureNormalBounds();
            _window.UpdateLayout();
            try { _window.DragMove(); } catch { }
        }
    }

    private Rect GetCurrentMonitorWorkAreaDip()
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
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

        var src = (HwndSource?)PresentationSource.FromVisual(_window);
        double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        double left = mi.rcWork.Left / scaleX;
        double top = mi.rcWork.Top / scaleY;
        double width = (mi.rcWork.Right - mi.rcWork.Left) / scaleX;
        double height = (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY;
        return new Rect(left, top, width, height);
    }

    private void ResizeBorder_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_window.WindowState == WindowState.Normal)
        {
            var pos = e.GetPosition(_window.ResizeBorder);
            var hit = GetResizeDirection(pos, _window.ResizeBorder.ActualWidth, _window.ResizeBorder.ActualHeight);
            _window.Cursor = GetCursorForResizeDirection(hit);
        }
        else
        {
            _window.Cursor = Cursors.Arrow;
        }
    }

    private void ResizeBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_window.WindowState == WindowState.Normal && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(_window.ResizeBorder);
            var hit = GetResizeDirection(pos, _window.ResizeBorder.ActualWidth, _window.ResizeBorder.ActualHeight);
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
        var hwnd = new WindowInteropHelper(_window).Handle;
        WindowInterop.SendMessage(hwnd, WM_NCLBUTTONDOWN, (nint)hit, nint.Zero);
    }
}
