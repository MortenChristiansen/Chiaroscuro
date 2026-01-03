using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System;

namespace BrowserHost.CefInfrastructure;

public class ResourceRequestHandler(PubSub pubSub, string tabId, bool isChildBrowser) : CefSharp.Handler.ResourceRequestHandler
{
    protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
    {
        var pageIsSuccessfullyLoaded = frame.IsMain && request.ResourceType == ResourceType.MainFrame && response.StatusCode == 200;
        if (pageIsSuccessfullyLoaded && !isChildBrowser)
            pubSub.Publish(new TabUrlLoadedSuccessfullyEvent(tabId));

        base.OnResourceLoadComplete(chromiumWebBrowser, browser, frame, request, response, status, receivedContentLength);
    }

    protected override CefReturnValue OnBeforeResourceLoad(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
    {
        // Block the default behavior that opens dropped files in the current tab
        if (request.TransitionType == TransitionType.LinkClicked && request.Url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            return CefReturnValue.Cancel;

        return base.OnBeforeResourceLoad(chromiumWebBrowser, browser, frame, request, callback);
    }
}
