using CefSharp;
using CefSharp.Handler;
using System;
using System.Collections.Generic;

namespace BrowserHost.CefInfrastructure;

public class CombinedDisplayHandler(Action<IList<string>> onFaviconAddressesChanged, Action<string> onStatusMessage) : DisplayHandler
{
    protected override void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> addresses)
    {
        onFaviconAddressesChanged(addresses);
    }

    protected override void OnStatusMessage(IWebBrowser chromiumWebBrowser, IBrowser browser, string value)
    {
        onStatusMessage(value ?? string.Empty);
    }
}