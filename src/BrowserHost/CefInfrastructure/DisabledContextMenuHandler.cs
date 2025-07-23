using CefSharp;
using CefSharp.Wpf.Handler;

namespace BrowserHost.CefInfrastructure;

/// <summary>
/// Context menu handler that disables all context menus for UI chrome browsers.
/// This prevents right-click menus from appearing on window chrome and UI elements.
/// </summary>
public class DisabledContextMenuHandler : ContextMenuHandler
{
    protected override void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
    {
        // Clear all menu items to disable the context menu
        model.Clear();
    }

    protected override bool OnContextMenuCommand(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        // Return true to indicate the command was handled (though no commands should reach here)
        return true;
    }

    protected override void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
    {
        // No action needed when context menu is dismissed
    }

    protected override bool RunContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
    {
        // Return true to suppress the context menu entirely
        return true;
    }
}
