using CefSharp;
using CefSharp.Handler;
using System;

namespace BrowserHost.CefInfrastructure;

public class StatusMessageDisplayHandler(Action<string> onStatusMessage) : DisplayHandler
{
    protected override void OnStatusMessage(IWebBrowser chromiumWebBrowser, IBrowser browser, string value)
    {
        onStatusMessage(value ?? string.Empty);
    }
}