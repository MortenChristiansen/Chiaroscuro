using CefSharp.Wpf;
using BrowserHost.Interop;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace BrowserHost.Features.ActionContext.Tabs;

public class SimpleBrowserWindow : Window
{
    private readonly ChromiumWebBrowser _browser;
    private Window? _ownerWindow;
    private double _offsetXPx;
    private double _offsetYPx;
    private bool _suppressOffsetUpdate;
    private const int CornerRadiusDip = 8;

    public SimpleBrowserWindow(string address)
    {
        Title = "New Window";
        Width = 1200;
        Height = 800;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;

        _browser = new ChromiumWebBrowser
        {
            Address = address,
        };

        var grid = new Grid();
        grid.Children.Add(_browser);
        Content = grid;

        Loaded += OnLoadedAttachToOwner;
        Loaded += (_, __) =>
        {
            TryHookDpiAndApplyRoundedCorners();
            ApplyRoundedWindowRegion();
        };
        LocationChanged += (_, __) => UpdateOffsets();
        Closed += (_, __) => DetachOwnerHandlers();
        SizeChanged += (_, __) => ApplyRoundedWindowRegion();
        StateChanged += (_, __) => ApplyRoundedWindowRegion();
    }

    private void OnLoadedAttachToOwner(object? sender, RoutedEventArgs e)
    {
        _ownerWindow = Owner ?? Window.GetWindow(MainWindow.Instance);
        if (_ownerWindow == null) return;

        // Initial position relative to owner, small offset
        var (ownerScaleX, ownerScaleY) = GetWindowScale(_ownerWindow);
        var (childScaleX, childScaleY) = GetWindowScale(this);

        if (double.IsNaN(Left) || double.IsNaN(Top) || (Left == 0 && Top == 0))
        {
            Left = _ownerWindow.Left + 40;
            Top = _ownerWindow.Top + 40;
        }
        var childLeftPx = Left * childScaleX;
        var childTopPx = Top * childScaleY;
        var ownerLeftPx = _ownerWindow.Left * ownerScaleX;
        var ownerTopPx = _ownerWindow.Top * ownerScaleY;
        _offsetXPx = childLeftPx - ownerLeftPx;
        _offsetYPx = childTopPx - ownerTopPx;

        _ownerWindow.LocationChanged += OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.SizeChanged += OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.StateChanged += OwnerWindow_StateChanged;
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
        var (ownerScaleX, ownerScaleY) = GetWindowScale(_ownerWindow);
        var (childScaleX, childScaleY) = GetWindowScale(this);
        var ownerLeftPx = _ownerWindow.Left * ownerScaleX;
        var ownerTopPx = _ownerWindow.Top * ownerScaleY;
        _suppressOffsetUpdate = true;
        try
        {
            Left = (ownerLeftPx + _offsetXPx) / childScaleX;
            Top = (ownerTopPx + _offsetYPx) / childScaleY;
        }
        finally { _suppressOffsetUpdate = false; }
    }

    private void UpdateOffsets()
    {
        if (_ownerWindow == null || _suppressOffsetUpdate) return;
        var (ownerScaleX, ownerScaleY) = GetWindowScale(_ownerWindow);
        var (childScaleX, childScaleY) = GetWindowScale(this);
        _offsetXPx = Left * childScaleX - _ownerWindow.Left * ownerScaleX;
        _offsetYPx = Top * childScaleY - _ownerWindow.Top * ownerScaleY;
    }

    private void DetachOwnerHandlers()
    {
        if (_ownerWindow == null) return;
        _ownerWindow.LocationChanged -= OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.SizeChanged -= OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.StateChanged -= OwnerWindow_StateChanged;
        _ownerWindow = null;
    }

    private static (double scaleX, double scaleY) GetWindowScale(Window w)
    {
        var src = (HwndSource?)PresentationSource.FromVisual(w);
        var scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        var scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
        return (scaleX, scaleY);
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
}
