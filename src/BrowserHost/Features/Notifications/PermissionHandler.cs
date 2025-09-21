using CefSharp;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public class PermissionHandler : CefSharp.Handler.PermissionHandler
{
    protected override bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
    {
        if (requestedPermissions.HasFlag(PermissionRequestType.Notifications))
        {
            return HandleNotificationPermissionRequest(requestingOrigin, callback);
        }

        return false; // Use default behavior for other permissions
    }

    protected override bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
    {
        return false; // Use default behavior for media permissions
    }

    private static bool HandleNotificationPermissionRequest(string requestingOrigin, IPermissionPromptCallback callback)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            var origin = new System.Uri(requestingOrigin).GetLeftPart(System.UriPartial.Authority);
            var result = NotificationPermissionDialog.ShowDialog(MainWindow.Instance, origin);

            var cefResult = result
                ? PermissionRequestResult.Accept
                : PermissionRequestResult.Deny;

            callback.Continue(cefResult);
        });
        return true;
    }
}
