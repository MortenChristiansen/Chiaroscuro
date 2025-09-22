using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.Settings;
using BrowserHost.Utilities;
using CefSharp;
using System;

namespace BrowserHost.CefInfrastructure;

public class ResourceRequestHandler(string tabId) : CefSharp.Handler.ResourceRequestHandler
{
    private bool _redirectChainActive;
    private string? _originalUrl;
    private string? _originalDomain;
    private bool _ssoFlowPublished;

    protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
    {
        var pageIsSuccessfullyLoaded = frame.IsMain && request.ResourceType == ResourceType.MainFrame && response.StatusCode == 200;
        if (pageIsSuccessfullyLoaded)
            PubSub.Publish(new TabUrlLoadedSuccessfullyEvent(tabId));

        var redirectChainTerminated = frame.IsMain && request.ResourceType == ResourceType.MainFrame && (response.StatusCode < 300 || response.StatusCode >= 400);
        if (redirectChainTerminated)
        {
            _redirectChainActive = false;
            _originalUrl = null;
            _originalDomain = null;
            _ssoFlowPublished = false;
        }


        base.OnResourceLoadComplete(chromiumWebBrowser, browser, frame, request, response, status, receivedContentLength);
    }

    protected override void OnResourceRedirect(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl)
    {
        if (frame.IsMain && request.ResourceType == ResourceType.MainFrame)
        {
            if (!_redirectChainActive)
            {
                _redirectChainActive = true;
                _originalUrl = request.Url;
                if (Uri.TryCreate(_originalUrl, UriKind.Absolute, out var fromUri))
                    _originalDomain = fromUri.Host;
            }

            // Detect SSO flow start when redirecting to login.microsoft.com
            if (!_ssoFlowPublished)
            {
                if (Uri.TryCreate(newUrl, UriKind.Absolute, out var toUri))
                {
                    if (string.Equals(toUri.Host, "login.microsoftonline.com", StringComparison.OrdinalIgnoreCase) && _originalDomain != null && _originalUrl != null)
                    {
                        _ssoFlowPublished = true;
                        PubSub.Publish(new SsoFlowStartedEvent(tabId, _originalDomain, _originalUrl));
                    }
                }
            }
        }

        base.OnResourceRedirect(chromiumWebBrowser, browser, frame, request, response, ref newUrl);
    }
}
