using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.ActionContext.Tabs;

public class RequestHandler(string tabId, bool isChildBrowser, PubSub pubSub) : CefSharp.Handler.RequestHandler
{
    protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
    {
        return new ResourceRequestHandler(pubSub, tabId, isChildBrowser);
    }

    protected override bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
    {
        if (targetDisposition == WindowOpenDisposition.NewBackgroundTab)
        {
            pubSub.Send(new StartNavigationCommand(targetUrl, UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));
            return true;
        }

        return false;
    }
}
