using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;

namespace BrowserHost.Features.Permissions;

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
        var deferral = e.GetDeferral();
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            try
            {
                var origin = Uri.TryCreate(e.Uri, UriKind.Absolute, out var parsed)
                    ? parsed.GetLeftPart(UriPartial.Authority)
                    : e.Uri;
                var permissionDisplay = ToFriendly(e.PermissionKind);
                var result = GenericPermissionDialog.ShowDialog(MainWindow.Instance, origin, permissionDisplay);
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

    private static string ToFriendly(CoreWebView2PermissionKind kind) => kind switch
    {
        CoreWebView2PermissionKind.Microphone => "Microphone",
        CoreWebView2PermissionKind.Camera => "Camera",
        CoreWebView2PermissionKind.Geolocation => "Geolocation",
        CoreWebView2PermissionKind.Notifications => "Notifications",
        CoreWebView2PermissionKind.OtherSensors => "Sensors",
        CoreWebView2PermissionKind.ClipboardRead => "Clipboard Read",
        CoreWebView2PermissionKind.MultipleAutomaticDownloads => "Multiple Downloads",
        CoreWebView2PermissionKind.FileReadWrite => "File Read/Write",
        CoreWebView2PermissionKind.Autoplay => "Autoplay",
        CoreWebView2PermissionKind.LocalFonts => "Local Fonts",
        CoreWebView2PermissionKind.MidiSystemExclusiveMessages => "MIDI (Sysex)",
        CoreWebView2PermissionKind.WindowManagement => "Window Management",
        _ => kind.ToString()
    };

    public void Dispose()
    {
        _browser.PermissionRequested -= Core_PermissionRequested;
    }
}
