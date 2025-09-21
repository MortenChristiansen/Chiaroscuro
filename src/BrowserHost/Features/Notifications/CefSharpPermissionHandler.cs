using CefSharp;
using System;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public class CefSharpPermissionHandler : CefSharp.Handler.PermissionHandler
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
            var origin = Uri.TryCreate(requestingOrigin, UriKind.Absolute, out var parsed)
                    ? parsed.GetLeftPart(UriPartial.Authority)
                    : requestingOrigin;
            var result = NotificationPermissionDialog.ShowDialog(MainWindow.Instance, origin);

            var cefResult = result
                ? PermissionRequestResult.Accept
                : PermissionRequestResult.Deny;

            callback.Continue(cefResult);
        });
        return true;
    }
}
