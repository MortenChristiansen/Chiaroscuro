using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.Tabs;

public class RequestHandler(string tabId) : CefSharp.Handler.RequestHandler
{
    protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
    {
        return new ResourceRequestHandler(tabId);
    }

    protected override bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
    {
        if (targetDisposition == WindowOpenDisposition.NewBackgroundTab)
        {
            PubSub.Publish(new NavigationStartedEvent(targetUrl, UseCurrentTab: false, SaveInHistory: true));
            return true;
        }

        return false;
    }
}
