using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;
using CefSharp;
using CefSharp.Handler;
using System.Windows;

namespace BrowserHost.Features.ActionContext.Tabs;

public record NotificationPermissionRequestedEvent(string TabId, string Origin);

public class PermissionHandler(string tabId) : CefSharp.Handler.PermissionHandler
{
    protected override bool OnRequestMediaAccessPermission(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string requestingUrl, int requestedPermissions, IMediaAccessCallback callback)
    {
        return false; // Use default behavior for media permissions
    }

    protected override bool OnShowPermissionPrompt(IWebBrowser chromiumWebBrowser, IBrowser browser, ulong promptId, string requestingUrl, int requestedPermissions, IPermissionPromptCallback callback)
    {
        // Handle notification permissions
        if ((requestedPermissions & (int)CefPermissionRequestTypes.Notifications) != 0)
        {
            return HandleNotificationPermissionRequest(requestingUrl, callback);
        }

        return false; // Use default behavior for other permissions
    }

    private bool HandleNotificationPermissionRequest(string requestingUrl, IPermissionPromptCallback callback)
    {
        var origin = new System.Uri(requestingUrl).GetLeftPart(System.UriPartial.Authority);
        var customization = TabCustomizationFeature.GetCustomizationsForTab(tabId);

        // Check if permission was already granted or denied
        switch (customization.NotificationPermission)
        {
            case NotificationPermissionStatus.Granted:
                callback.Continue(CefPermissionRequestResult.Accept);
                return true;

            case NotificationPermissionStatus.Denied:
                callback.Continue(CefPermissionRequestResult.Deny);
                return true;

            case NotificationPermissionStatus.NotAsked:
            default:
                // Show permission dialog to user
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    var result = MessageBox.Show(
                        $"The website {origin} wants to show notifications. Do you want to allow this?",
                        "Notification Permission Request",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    var permission = result == MessageBoxResult.Yes 
                        ? NotificationPermissionStatus.Granted 
                        : NotificationPermissionStatus.Denied;

                    // Save the permission choice
                    PubSub.Publish(new TabNotificationPermissionChangedEvent(tabId, permission));

                    // Respond to the permission request
                    var cefResult = result == MessageBoxResult.Yes 
                        ? CefPermissionRequestResult.Accept 
                        : CefPermissionRequestResult.Deny;
                    
                    callback.Continue(cefResult);
                });
                return true;
        }
    }
}