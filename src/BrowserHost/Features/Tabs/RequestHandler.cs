using CefSharp;

namespace BrowserHost.Features.Tabs;

public class RequestHandler(string tabId) : CefSharp.Handler.RequestHandler
{
    protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
    {
        return new ResourceRequestHandler(tabId);
    }
}
