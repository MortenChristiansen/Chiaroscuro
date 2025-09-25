using BrowserHost.Interop;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace BrowserHost;

public partial class MainWindow
{
    private const int MinWindowWidth = 400;
    private const int MinWindowHeight = 300;
    private const byte AcrylicTintOpacity = 0x2F; // 0x00..0xFF (higher = more solid tint)

    public override void EndInit()
    {
        Loaded += (_, __) =>
        {
            TryEnableSystemBackdrop();
            ApplyRoundedWindowRegion();

            var src = (HwndSource?)PresentationSource.FromVisual(this);
            src?.AddHook(WndProc);
        };
        SizeChanged += (_, __) => ApplyRoundedWindowRegion();
        StateChanged += (_, __) => ApplyRoundedWindowRegion();

        base.EndInit();
    }

    private void TryEnableSystemBackdrop()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        try
        {
            // Prefer Windows 11 system backdrop (Mica)
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
            {
                var backdropType = 2; // DWMSBT_MAINWINDOW
                _ = WindowInterop.DwmSetWindowAttribute(hwnd, WindowInterop.DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));

                // Request rounded corners (DWM_WINDOW_CORNER_PREFERENCE_ROUND = 2)
                var cornerPref = 2;
                _ = WindowInterop.DwmSetWindowAttribute(hwnd, WindowInterop.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

                // Also enable legacy Mica flag for older 22000 builds
                var enable = 1;
                _ = WindowInterop.DwmSetWindowAttribute(hwnd, WindowInterop.DWMWA_MICA_EFFECT, ref enable, sizeof(int));

                var useDark = 1;
                _ = WindowInterop.DwmSetWindowAttribute(hwnd, WindowInterop.DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));

                return;
            }
        }
        catch when (!Debugger.IsAttached) { }

    }

    private static void EnableAccentBlur(IntPtr hwnd, Color color, byte opacity)
    {
        var accent = new WindowInterop.ACCENT_POLICY
        {
            AccentState = WindowInterop.ACCENT_STATE.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 0, // Is ignored
            // GradientColor format: 0xAABBGGRR (alpha in high byte)
            GradientColor = ((uint)opacity << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R,
            AnimationId = 0
        };

        var size = Marshal.SizeOf<WindowInterop.ACCENT_POLICY>();
        var pAccent = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, pAccent, false);
            var data = new WindowInterop.WINDOWCOMPOSITIONATTRIBDATA
            {
                Attribute = WindowInterop.WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                Data = pAccent,
                SizeOfData = size
            };
            _ = WindowInterop.SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(pAccent);
        }
    }

    private void SetBackgroundColor(Color color)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        EnableAccentBlur(hwnd, color, AcrylicTintOpacity);
    }

    private DispatcherTimer? _tintTimer;

    private void AnimateBackgroundColor(Color from, Color to)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        if (from == to)
        {
            SetBackgroundColor(to);
            return;
        }

        // Ensure DWM starts from the expected color before animating (prevents first-tick jump)
        EnableAccentBlur(hwnd, from, AcrylicTintOpacity);

        // Stop any in-flight animation to avoid overlapping updates
        _tintTimer?.Stop();

        var sw = Stopwatch.StartNew();
        var duration = TimeSpan.FromMilliseconds(500);
        _tintTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _tintTimer.Tick += (_, __) =>
        {
            var t = Math.Clamp(sw.Elapsed.TotalMilliseconds / duration.TotalMilliseconds, 0.0, 1.0);
            // Ease-in-out cubic for smoother mid-phase
            var e = t < 0.5 ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;

            var r = (byte)(from.R + (to.R - from.R) * e);
            var g = (byte)(from.G + (to.G - from.G) * e);
            var b = (byte)(from.B + (to.B - from.B) * e);
            var c = Color.FromRgb(r, g, b);

            EnableAccentBlur(hwnd, c, AcrylicTintOpacity);

            if (t >= 1.0)
            {
                _tintTimer!.Stop();
            }
        };
        _tintTimer.Start();
    }

    // ===== Rounded window region to clip blur at corners =====
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
                    // OS did not take ownership; free to avoid leak
                    WindowInterop.DeleteObject(rgn);
                }
                // else: ownership transfers to the window
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"ApplyRoundedWindowRegion failed: {ex.Message}");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_DPICHANGED = 0x02E0;
        const int WM_GETMINMAXINFO = 0x0024;

        if (msg == WM_DPICHANGED)
        {
            Dispatcher.BeginInvoke(ApplyRoundedWindowRegion);
        }
        else if (msg == WM_GETMINMAXINFO)
        {
            try
            {
                var mmi = Marshal.PtrToStructure<MonitorInterop.MINMAXINFO>(lParam);

                // Determine monitor work area (excludes taskbar) & full monitor area
                var monitor = MonitorInterop.MonitorFromWindow(hwnd, MonitorInterop.MONITOR_DEFAULTTONEAREST);
                var mi = new MonitorInterop.MONITORINFO { cbSize = Marshal.SizeOf<MonitorInterop.MONITORINFO>() };
                if (MonitorInterop.GetMonitorInfo(monitor, ref mi))
                {
                    int monitorWidth = mi.rcMonitor.Right - mi.rcMonitor.Left;
                    int monitorHeight = mi.rcMonitor.Bottom - mi.rcMonitor.Top;
                    int workWidth = mi.rcWork.Right - mi.rcWork.Left;
                    int workHeight = mi.rcWork.Bottom - mi.rcWork.Top;

                    // Position of the maximized window relative to the monitor
                    mmi.ptMaxPosition.X = mi.rcWork.Left - mi.rcMonitor.Left;
                    mmi.ptMaxPosition.Y = mi.rcWork.Top - mi.rcMonitor.Top;
                    mmi.ptMaxSize.X = workWidth;
                    mmi.ptMaxSize.Y = workHeight;

                    // Min tracking size (convert desired DIP min size to physical pixels)
                    var src = (HwndSource?)HwndSource.FromHwnd(hwnd);
                    double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
                    double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
                    mmi.ptMinTrackSize.X = (int)(MinWindowWidth * scaleX);
                    mmi.ptMinTrackSize.Y = (int)(MinWindowHeight * scaleY);

                    // Max tracking size limited to monitor size
                    mmi.ptMaxTrackSize.X = monitorWidth;
                    mmi.ptMaxTrackSize.Y = monitorHeight;

                    Marshal.StructureToPtr(mmi, lParam, true);
                    handled = false; // Let default proc continue, we just adjusted values
                }
            }
            catch { }
        }

        return IntPtr.Zero;
    }
}
