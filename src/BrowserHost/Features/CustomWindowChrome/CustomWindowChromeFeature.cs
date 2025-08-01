﻿using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeFeature(MainWindow window) : Feature(window)
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
        Window.WindowState = Window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Minimize()
    {
        Window.WindowState = WindowState.Minimized;
    }

    private double _lastX = 0;
    private double _lastY = 0;

    private void AdjustWindowBorder()
    {
        if (Window.WindowState == WindowState.Maximized)
        {
            _lastX = Window.Left;
            _lastY = Window.Top;

            var wa = SystemParameters.WorkArea;
            // Set the window's max size to the work area to prevent overlaying the taskbar
            Window.MaxWidth = wa.Width + 11;
            Window.MaxHeight = wa.Height + 11;
            Window.Left = wa.Left + 1;
            Window.Top = wa.Top + 1;
        }
        else if (Window.WindowState == WindowState.Normal)
        {
            Window.MaxWidth = double.PositiveInfinity;
            Window.MaxHeight = double.PositiveInfinity;

            // Restore the last position when switching back to normal state
            Window.Left = _lastX;
            Window.Top = _lastY;
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
    private static extern nint SendMessage(nint hWnd, int msg, nint wParam, nint lParam);
    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private void ResizeWindow(HitTest hit)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(Window).Handle;
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (nint)hit, nint.Zero);
    }

    #endregion
}
