using BrowserHost.Interop;
using CefSharp;
using CefSharp.Handler;
using System.Windows;
using System.Windows.Media;

namespace BrowserHost.Features.WebContextMenu;
public partial class WebContentContextMenuHandler : ContextMenuHandler
{
    private WebContextMenuWindow? _contextWindow;

    protected override void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
    {
        // Clear all menu items to disable the default context menu
        model.Clear();

        var relativeX = parameters.XCoord;
        var relativeY = parameters.YCoord;

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CloseExistingInstance();

            var owner = MainWindow.Instance;

            var cursorPos = GetCursorPositionInDips(owner);
            var offset = GetDpiAwareOffset(owner, 12, 12); // 12px right and down, scaled for DPI
            _contextWindow = new WebContextMenuWindow(owner, cursorPos.X + offset.X, cursorPos.Y + offset.Y);
            _contextWindow.Show();
        });
    }

    // TODO: We may eventually want to reuse the existing context menu window instead of closing and reopening it.
    private void CloseExistingInstance()
    {
        if (_contextWindow != null)
        {
            try { _contextWindow.Close(); } catch { }
            _contextWindow = null;
        }
    }

    private static Point GetCursorPositionInDips(Visual referenceVisual)
    {
        if (!MonitorInterop.GetCursorPos(out var pt))
            return new Point(0, 0);

        var pixelPoint = new Point(pt.X, pt.Y);
        var source = PresentationSource.FromVisual(referenceVisual);
        if (source?.CompositionTarget is null)
            return pixelPoint; // Fallback; may be off on high-DPI

        var transformFromDevice = source.CompositionTarget.TransformFromDevice;
        return transformFromDevice.Transform(pixelPoint);
    }

    private static Vector GetDpiAwareOffset(Visual referenceVisual, double pixelX, double pixelY)
    {
        var source = PresentationSource.FromVisual(referenceVisual);
        if (source?.CompositionTarget is null)
            return new Vector(pixelX, pixelY); // Fallback; may be off on high-DPI

        var transformFromDevice = source.CompositionTarget.TransformFromDevice;
        var dip = transformFromDevice.Transform(new Point(pixelX, pixelY));
        return new Vector(dip.X, dip.Y);
    }


    protected override bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
    {
        // Return true to suppress the default context menu
        return true;
    }

    protected override void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
    {
    }

    protected override bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        return true;
    }
}
