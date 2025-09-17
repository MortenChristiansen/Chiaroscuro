using BrowserHost.Interop;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace BrowserHost.Tab.WebView2;

internal sealed class WebView2RoundedCornerManager
{
    private readonly int _cornerRadiusPx;
    private IntPtr _parentHwnd;
    private IntPtr _childWebViewHwnd;
    private bool _enumeratedChild;

    public WebView2RoundedCornerManager(int cornerRadiusPx)
    {
        _cornerRadiusPx = cornerRadiusPx;
    }

    public void SetParentWindowHandle(IntPtr hwnd)
    {
        if (_parentHwnd != hwnd)
        {
            _parentHwnd = hwnd;
            _enumeratedChild = false;
            _childWebViewHwnd = IntPtr.Zero;
        }
    }

    public void EnsureChildWindowAsync(Dispatcher dispatcher, Func<int> getWidth, Func<int> getHeight)
    {
        if (_enumeratedChild || _parentHwnd == IntPtr.Zero) return;
        _enumeratedChild = true;
        dispatcher.BeginInvoke(async () =>
        {
            for (var i = 0; i < 6 && _childWebViewHwnd == IntPtr.Zero; i++)
            {
                EnumerateChildWindows();
                if (_childWebViewHwnd != IntPtr.Zero) break;
                await Task.Delay(50);
            }
            if (_childWebViewHwnd != IntPtr.Zero)
            {
                ApplyRoundedRegion(getWidth(), getHeight());
            }
        });
    }

    public void ApplyRoundedRegion(int width, int height)
    {
        if (_childWebViewHwnd == IntPtr.Zero || width <= 0 || height <= 0) return;
        var r = _cornerRadiusPx * 2;
        var region = WindowInterop.CreateRoundRectRgn(0, 0, width + 1, height + 1, r, r);
        if (region != IntPtr.Zero)
        {
            var result = WindowInterop.SetWindowRgn(_childWebViewHwnd, region, true);
            if (result == 0)
            {
                // OS did not take ownership; delete to prevent leak
                WindowInterop.DeleteObject(region);
            }
        }
    }

    private void EnumerateChildWindows()
    {
        WindowInterop.EnumChildWindows(_parentHwnd, (hWnd, _) =>
        {
            var sb = new StringBuilder(256);
            _ = WindowInterop.GetClassName(hWnd, sb, sb.Capacity);
            var cls = sb.ToString();
            if (cls.StartsWith("Chrome_WidgetWin_", StringComparison.Ordinal))
            {
                _childWebViewHwnd = hWnd;
                return false; // stop enumeration
            }
            return true; // continue
        }, IntPtr.Zero);
    }
}
