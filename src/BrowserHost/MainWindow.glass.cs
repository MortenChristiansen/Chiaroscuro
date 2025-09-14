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
                _ = DwmSetWindowAttribute(hwnd, 38, ref backdropType, sizeof(int)); // DWMWA_SYSTEMBACKDROP_TYPE = 38

                // Request rounded corners (DWMWA_WINDOW_CORNER_PREFERENCE = 33, DWM_WINDOW_CORNER_PREFERENCE_ROUND = 2)
                var cornerPref = 2;
                _ = DwmSetWindowAttribute(hwnd, 33, ref cornerPref, sizeof(int));

                // Also enable legacy Mica flag (DWMWA_MICA_EFFECT = 1029) for older 22000 builds
                var enable = 1;
                _ = DwmSetWindowAttribute(hwnd, 1029, ref enable, sizeof(int));

                // Optional: dark mode hint
                int useDark = 1;
                _ = DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int)); // DWMWA_USE_IMMERSIVE_DARK_MODE = 20

                return;
            }
        }
        catch when (!Debugger.IsAttached) { }

    }

    private enum WINDOWCOMPOSITIONATTRIB
    {
        WCA_ACCENT_POLICY = 19
    }

    private enum ACCENT_STATE
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
        ACCENT_INVALID_STATE = 5
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ACCENT_POLICY
    {
        public ACCENT_STATE AccentState;
        public int AccentFlags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public WINDOWCOMPOSITIONATTRIB Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private static void EnableAccentBlur(IntPtr hwnd, Color color, byte opacity)
    {
        var accent = new ACCENT_POLICY
        {
            AccentState = ACCENT_STATE.ACCENT_ENABLE_ACRYLICBLURBEHIND,
            AccentFlags = 0, // Is ignored
            // GradientColor format: 0xAABBGGRR (alpha in high byte)
            GradientColor = ((uint)opacity << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R,
            AnimationId = 0
        };

        var size = Marshal.SizeOf<ACCENT_POLICY>();
        var pAccent = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, pAccent, false);
            var data = new WINDOWCOMPOSITIONATTRIBDATA
            {
                Attribute = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                Data = pAccent,
                SizeOfData = size
            };
            _ = SetWindowCompositionAttribute(hwnd, ref data);
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

            IntPtr rgn = CreateRoundRectRgn(0, 0, w + 1, h + 1, r * 2, r * 2);
            if (rgn != IntPtr.Zero)
            {
                SetWindowRgn(hwnd, rgn, true);
                // Ownership of rgn transfers to the window
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

        if (msg == WM_DPICHANGED)
        {
            Dispatcher.BeginInvoke(ApplyRoundedWindowRegion);
        }

        return IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [LibraryImport("gdi32.dll")]
    private static partial IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

    [LibraryImport("user32.dll")]
    private static partial int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, [MarshalAs(UnmanagedType.Bool)] bool bRedraw);

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [LibraryImport("user32.dll")]
    private static partial int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);
}
