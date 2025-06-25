using CefSharp;
using CefSharp.Handler;
using System;
using System.Collections.Generic;

namespace BrowserHost.CefInfrastructure;

public class FaviconDisplayHandler(Action<IList<string>> onFaviconAddressesChanged) : DisplayHandler
{
    protected override void OnFaviconUrlChange(IWebBrowser chromiumWebBrowser, IBrowser browser, IList<string> addresses)
    {
        onFaviconAddressesChanged(addresses);
    }
}
