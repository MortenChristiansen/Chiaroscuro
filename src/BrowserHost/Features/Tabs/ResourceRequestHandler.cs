using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.Tabs;

public class ResourceRequestHandler(string tabId) : CefSharp.Handler.ResourceRequestHandler
{
    protected override void OnResourceLoadComplete(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
    {
        var pageIsSuccessfullyLoaded = frame.IsMain && request.ResourceType == ResourceType.MainFrame && response.StatusCode == 200;
        if (pageIsSuccessfullyLoaded)
            PubSub.Publish(new TabUrlLoadedSuccessfullyEvent(tabId));

        base.OnResourceLoadComplete(chromiumWebBrowser, browser, frame, request, response, status, receivedContentLength);
    }
}
