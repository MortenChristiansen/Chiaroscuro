using BrowserHost.Api;
using CefSharp;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features;

public class CustomWindowChromeFeature(MainWindow window, BrowserApi api) : Feature(window, api)
{
    public override void Register()
    {
        ConfigureUiControl("ChromeUI", "/", Window.ChromeUI);

        Window.WindowStyle = WindowStyle.None;
        Window.AllowsTransparency = true;

        Window.ChromeUI.PreviewMouseLeftButtonDown += ChromeUI_PreviewMouseLeftButtonDown;
        Window.PreviewMouseLeftButtonUp += MainWindow_PreviewMouseLeftButtonUp;
        Window.PreviewMouseMove += MainWindow_PreviewMouseMove;

        Window.ResizeBorder.PreviewMouseMove += ResizeBorder_PreviewMouseMove;
        Window.ResizeBorder.PreviewMouseLeftButtonDown += ResizeBorder_PreviewMouseLeftButtonDown;

        // Force WebContent to repaint on size change to fix rendering issue
        Window.SizeChanged += (s, e) => RedrawBrowsers();

        // Prevent maximizing over the taskbar
        Window.StateChanged += (s, e) => AdjustWindowBorder();

        _ = Listen(Api.WindowMinimizedChannel, _ => Minimize(), dispatchToUi: true);
        _ = Listen(Api.WindowStateToggledChannel, _ => ToggleMaximizedState(), dispatchToUi: true);
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

    #region Minimize/Maximize

    private void ToggleMaximizedState()
    {
        Window.WindowState = Window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        RedrawBrowsers();
    }

    private void Minimize()
    {
        Window.WindowState = WindowState.Minimized;
    }

    private void AdjustWindowBorder()
    {
        if (Window.WindowState == WindowState.Maximized)
        {
            var wa = SystemParameters.WorkArea;
            var bottomMargin = Window.Height - wa.Height - 10;
            Window.WindowBorder.Margin = new Thickness(0, 0, 0, bottomMargin);
        }
        else if (Window.WindowState == WindowState.Normal)
        {
            Window.WindowBorder.Margin = new Thickness(0);
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

        // Get mouse position in screen coordinates
        var mouseScreen = Mouse.GetPosition(null);
        var presentationSource = PresentationSource.FromVisual(Window);
        if (presentationSource != null)
        {
            var transform = presentationSource.CompositionTarget.TransformToDevice;
            var screenX = mouseScreen.X * transform.M11;
            var screenY = mouseScreen.Y * transform.M22;
            var newLeft = screenX - Window.ActualWidth * percentX;
            var newTop = screenY - Window.ActualHeight * percentY;

            // Clamp newLeft to the primary screen's working area
            double minLeft = 0;
            double maxLeft = SystemParameters.WorkArea.Width - Window.ActualWidth;
            if (newLeft < minLeft) newLeft = minLeft;
            if (newLeft > maxLeft) newLeft = maxLeft;

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
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private void ResizeWindow(HitTest hit)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(Window).Handle;
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)hit, IntPtr.Zero);
    }

    #endregion
}
