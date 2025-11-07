using BrowserHost.XamlUtilities;
using CefSharp;
using CefSharp.Handler;
using System.Windows;

namespace BrowserHost.Features.WebContextMenu;

public partial class WebContentContextMenuHandler : ContextMenuHandler
{
    protected override void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
    {
        // Clear all menu items to disable the default context menu
        model.Clear();

        var mappedParameters = Map(parameters);

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            var owner = MainWindow.Instance;

            var cursorPos = VisualDpiUtil.GetCursorPositionInDips(owner);
            var offset = VisualDpiUtil.GetDpiAwareOffset(owner, 12, 12); // 12px right and down, scaled for DPI
            var window = new WebContextMenuWindow(owner, cursorPos.X + offset.X, cursorPos.Y + offset.Y);
            window.Prepare(mappedParameters);
            window.Show();
            window.Activate(); // Ensure the menu gets focus so Deactivated will fire on outside click

            window.Deactivated += ContextWindow_Deactivated;
            void ContextWindow_Deactivated(object? s, System.EventArgs e)
            {
                window!.Deactivated -= ContextWindow_Deactivated;
                try // If closed elsewhere this will throw
                {
                    window.Close();
                }
                catch
                { }
            }
        });
    }

    private static ContextMenuParameters Map(IContextMenuParams parameters) =>
        new(parameters.LinkUrl);

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
