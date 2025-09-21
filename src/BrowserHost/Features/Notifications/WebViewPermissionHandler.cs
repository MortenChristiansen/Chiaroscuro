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

    public static IDisposable Register(CoreWebView2 browser) =>
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
                var origin = Uri.TryCreate(e.Uri, UriKind.Absolute, out var parsed)
                    ? parsed.GetLeftPart(UriPartial.Authority)
                    : e.Uri;
                var result = NotificationPermissionDialog.ShowDialog(MainWindow.Instance, origin);
                e.State = result ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;
            }
            catch
            {
                e.State = CoreWebView2PermissionState.Deny;
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
