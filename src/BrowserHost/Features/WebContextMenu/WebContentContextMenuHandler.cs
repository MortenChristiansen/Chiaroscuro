using BrowserHost.XamlUtilities;
using CefSharp;
using CefSharp.Handler;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost.Features.WebContextMenu;

public partial class WebContentContextMenuHandler : ContextMenuHandler
{
    private WebContextMenuWindow? _contextWindow;
    private Window? _ownerHookedWindow;

    protected override void OnBeforeContextMenu(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
    {
        // Clear all menu items to disable the default context menu
        model.Clear();

        var relativeX = parameters.XCoord;
        var relativeY = parameters.YCoord;
        var mappedParameters = Map(parameters);

        Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            CloseExistingInstance();

            var owner = MainWindow.Instance;

            var cursorPos = VisualDpiUtil.GetCursorPositionInDips(owner);
            var offset = VisualDpiUtil.GetDpiAwareOffset(owner, 12, 12); // 12px right and down, scaled for DPI
            _contextWindow = new WebContextMenuWindow(owner, cursorPos.X + offset.X, cursorPos.Y + offset.Y);
            _contextWindow.Prepare(mappedParameters);
            _contextWindow.Show();

            AttachOutsideClickHandlers(owner);
        });
    }

    private static ContextMenuParameters Map(IContextMenuParams parameters) =>
        new(parameters.LinkUrl);

    private void CloseExistingInstance()
    {
        DetachOutsideClickHandlers();
        if (_contextWindow != null)
        {
            try { _contextWindow.Close(); } catch { }
            _contextWindow = null;
        }
    }


    private void AttachOutsideClickHandlers(Window owner)
    {
        if (_ownerHookedWindow != owner)
        {
            DetachOutsideClickHandlers();
            _ownerHookedWindow = owner;
            _ownerHookedWindow.PreviewMouseDown += OtherWindow_PreviewMouseDown;

            foreach (var window in OverlayWindow.Instances.Where(i => i != _contextWindow))
                window.PreviewMouseDown += OtherWindow_PreviewMouseDown;
        }

        if (_contextWindow != null)
        {
            _contextWindow.Deactivated += ContextWindow_Deactivated;
        }
    }

    private void DetachOutsideClickHandlers()
    {
        if (_ownerHookedWindow != null)
        {
            _ownerHookedWindow.PreviewMouseDown -= OtherWindow_PreviewMouseDown;
            _ownerHookedWindow = null;

            foreach (var window in OverlayWindow.Instances.Where(i => i != _contextWindow))
                window.PreviewMouseDown -= OtherWindow_PreviewMouseDown;
        }

        if (_contextWindow != null)
        {
            _contextWindow.Deactivated -= ContextWindow_Deactivated;
        }
    }

    // Click occurred on another window; since the context menu is a separate overlay window,
    // any click on another window is outside the menu, so close it.
    private void OtherWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e) =>
        CloseExistingInstance();

    // If the overlay window loses activation (e.g., click on another app), close it.
    private void ContextWindow_Deactivated(object? sender, System.EventArgs e) =>
        CloseExistingInstance();

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
