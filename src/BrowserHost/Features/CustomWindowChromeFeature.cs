using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features;

public class CustomWindowChromeFeature(MainWindow window)
{
    private readonly List<ChromiumWebBrowser> _browsers = [window.WebContent, window.ChromeUI, window.ActionDialog];

    public void Register()
    {
        window.WindowStyle = WindowStyle.None;
        window.AllowsTransparency = true;

        window.ChromeUI.PreviewMouseLeftButtonDown += ChromeUI_PreviewMouseLeftButtonDown;
        window.PreviewMouseLeftButtonUp += MainWindow_PreviewMouseLeftButtonUp;
        window.PreviewMouseMove += MainWindow_PreviewMouseMove;

        window.ResizeBorder.PreviewMouseMove += ResizeBorder_PreviewMouseMove;
        window.ResizeBorder.PreviewMouseLeftButtonDown += ResizeBorder_PreviewMouseLeftButtonDown;

        // Force WebContent to repaint on size change to fix rendering issue
        window.SizeChanged += (s, e) => RedrawBrowsers();

        // Prevent maximizing over the taskbar
        window.StateChanged += (s, e) => AdjustWindowBorder();
    }

    private void RedrawBrowsers() =>
        _browsers.ForEach(b => b.GetBrowserHost()?.Invalidate(PaintElementType.View));

    private void ChromeUI_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && IsMouseOverTransparentPixel(e))
        {
            ToggleMaximizedState();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed && IsMouseOverTransparentPixel(e))
        {
            if (window.WindowState == WindowState.Maximized)
                HandleDragToDetachFromMaximizedState(e);

            window.DragMove();
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

    #region Minimize/Maximize

    public void ToggleMaximizedState()
    {
        window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        RedrawBrowsers();
    }

    public void Minimize()
    {
        window.WindowState = WindowState.Minimized;
    }

    private void AdjustWindowBorder()
    {
        if (window.WindowState == WindowState.Maximized)
        {
            var wa = SystemParameters.WorkArea;
            var bottomMargin = window.Height - wa.Height - 10;
            window.WindowBorder.Margin = new Thickness(0, 0, 0, bottomMargin);
        }
        else if (window.WindowState == WindowState.Normal)
        {
            window.WindowBorder.Margin = new Thickness(0);
        }
    }

    #endregion

    #region Window Detach Drag Handling

    private bool _isDraggingToDetach = false;
    private Point? _dragStartPoint;
    private void HandleDragToDetachFromMaximizedState(MouseEventArgs e)
    {
        if (_isDraggingToDetach && _dragStartPoint.HasValue)
        {
            // If already dragging, calculate the distance moved
            var currentPoint = e.GetPosition(window);
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
        window.ChromeUI.ReleaseMouseCapture();
        window.DragMove();
    }

    private void PerformDetachOfWindow(MouseEventArgs e)
    {
        // Calculate mouse position relative to window
        var mouseX = e.GetPosition(window).X;
        var percentX = mouseX / window.ActualWidth;
        var mouseY = e.GetPosition(window).Y;
        var percentY = mouseY / window.ActualHeight;

        // Set to normal state
        window.WindowState = WindowState.Normal;

        // Get mouse position in screen coordinates
        var mouseScreen = Mouse.GetPosition(null);
        var presentationSource = PresentationSource.FromVisual(window);
        if (presentationSource != null)
        {
            var transform = presentationSource.CompositionTarget.TransformToDevice;
            var screenX = mouseScreen.X * transform.M11;
            var screenY = mouseScreen.Y * transform.M22;
            var newLeft = screenX - window.ActualWidth * percentX;
            var newTop = screenY - window.ActualHeight * percentY;

            // Clamp newLeft to the primary screen's working area
            double minLeft = 0;
            double maxLeft = SystemParameters.WorkArea.Width - window.ActualWidth;
            if (newLeft < minLeft) newLeft = minLeft;
            if (newLeft > maxLeft) newLeft = maxLeft;

            window.Left = newLeft;
            window.Top = newTop;
        }

        window.UpdateLayout();
    }

    private void StartDetachDrag(MouseEventArgs e)
    {
        _dragStartPoint = e.GetPosition(window);
        _isDraggingToDetach = true;
        window.Cursor = Cursors.SizeAll;
        // Prevents selecting elements as we complete the detach operation
        window.ChromeUI.EvaluateScriptAsync("document.body.setAttribute('inert', '');").GetAwaiter().GetResult();
    }

    private void ResetDetachDrag()
    {
        _dragStartPoint = null;
        _isDraggingToDetach = false;
        window.Cursor = Cursors.Arrow;
        window.ChromeUI.EvaluateScriptAsync("document.body.removeAttribute('inert');").GetAwaiter().GetResult();
    }

    #endregion

    #region Resize Border Handling

    private void ResizeBorder_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (window.WindowState == WindowState.Normal)
        {
            var pos = e.GetPosition(window.ResizeBorder);
            var hit = GetResizeDirection(pos, window.ResizeBorder.ActualWidth, window.ResizeBorder.ActualHeight);
            window.Cursor = GetCursorForResizeDirection(hit);
        }
    }

    private void ResizeBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (window.WindowState == WindowState.Normal && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(window.ResizeBorder);
            var hit = GetResizeDirection(pos, window.ResizeBorder.ActualWidth, window.ResizeBorder.ActualHeight);
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
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private void ResizeWindow(HitTest hit)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)hit, IntPtr.Zero);
    }

    #endregion
}
