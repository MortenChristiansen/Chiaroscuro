using BrowserHost.Features.Tabs;
using CefSharp;
using System;

namespace BrowserHost.Auth;

public class WinAuthHandler(string tabId) : RequestHandler(tabId)
{
    protected override bool GetAuthCredentials(IWebBrowser chromiumWebBrowser, IBrowser browser, string originUrl, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
    {
        if (scheme.Equals("ntlm", StringComparison.OrdinalIgnoreCase) || scheme.Equals("negotiate", StringComparison.OrdinalIgnoreCase))
        {
            // Try default credentials
            callback.Continue(Environment.UserName, "");
            return true;
        }

        return base.GetAuthCredentials(
            chromiumWebBrowser,
            browser,
            originUrl,
            isProxy,
            host,
            port,
            realm,
            scheme,
            callback
        );
    }
}
