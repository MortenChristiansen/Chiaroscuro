using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;

namespace BrowserHost.Features.Notifications;

public sealed class WebViewPermissionHandler : IDisposable
{
    private readonly CoreWebView2 _browser;

    private WebViewPermissionHandler(CoreWebView2 browser)
    {
        _browser = browser;
        browser.PermissionRequested += Core_PermissionRequested;
    }

    public static WebViewPermissionHandler Register(CoreWebView2 browser) =>
        new WebViewPermissionHandler(browser);

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

    public void Dispose()
    {
        _browser.PermissionRequested -= Core_PermissionRequested;
    }
}
