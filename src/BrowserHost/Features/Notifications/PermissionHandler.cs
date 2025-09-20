using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using CefSharp;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public record NotificationPermissionRequestedEvent(string TabId, string Origin);

public class PermissionHandler(string tabId) : CefSharp.Handler.PermissionHandler
{
    protected override bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingOrigin, MediaAccessPermissionType requestedPermissions, IMediaAccessCallback callback)
    {
        return false; // Use default behavior for media permissions
    }

    protected override bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingOrigin, PermissionRequestType requestedPermissions, IPermissionPromptCallback callback)
    {
        // Handle notification permissions
        if (requestedPermissions.HasFlag(PermissionRequestType.Notifications))
        {
            return HandleNotificationPermissionRequest(requestingOrigin, callback);
        }

        return false; // Use default behavior for other permissions
    }

    private bool HandleNotificationPermissionRequest(string requestingOrigin, IPermissionPromptCallback callback)
    {
        var origin = new System.Uri(requestingOrigin).GetLeftPart(System.UriPartial.Authority);
        var customization = TabCustomizationFeature.GetCustomizationsForTab(tabId);

        // Check if permission was already granted or denied
        switch (customization.NotificationPermission)
        {
            case NotificationPermissionStatus.Granted:
                callback.Continue(PermissionRequestResult.Accept);
                return true;

            case NotificationPermissionStatus.Denied:
                callback.Continue(PermissionRequestResult.Deny);
                return true;

            case NotificationPermissionStatus.NotAsked:
            default:
                // Show permission dialog to user
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var result = NotificationPermissionDialog.ShowDialog(MainWindow.Instance, origin);

                    var permission = result
                        ? NotificationPermissionStatus.Granted
                        : NotificationPermissionStatus.Denied;

                    // Save the permission choice
                    PubSub.Publish(new TabNotificationPermissionChangedEvent(tabId, permission));

                    // Update JavaScript permission status
                    var browser = MainWindow.Instance.CurrentTab as IWebBrowser;
                    if (browser != null)
                    {
                        NotificationApiInjector.UpdatePermissionStatus(browser, permission);
                    }

                    // Respond to the permission request
                    var cefResult = result
                        ? PermissionRequestResult.Accept
                        : PermissionRequestResult.Deny;

                    callback.Continue(cefResult);
                });
                return true;
        }
    }
}