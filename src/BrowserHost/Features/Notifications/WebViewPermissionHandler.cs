using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public static class WebViewPermissionHandler
{
    public static void Register(CoreWebView2 browser)
    {
        browser.PermissionRequested += Core_PermissionRequested;
    }

    private static void Core_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (e.PermissionKind != CoreWebView2PermissionKind.Notifications)
            return; // let default behavior handle other permissions

        var deferral = e.GetDeferral();
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var origin = new Uri(e.Uri).GetLeftPart(UriPartial.Authority);
                var result = NotificationPermissionDialog.ShowDialog(MainWindow.Instance, origin);
                e.State = result ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
            }
            catch
            {
                e.State = CoreWebView2PermissionState.Default;
            }
            finally
            {
                deferral.Complete();
            }
        });
    }
}
