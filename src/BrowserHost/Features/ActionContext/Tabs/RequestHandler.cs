using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using CefSharp;
using System;

namespace BrowserHost.Features.ActionContext.Tabs;

public class RequestHandler(string tabId, bool isChildBrowser, PubSub pubSub) : CefSharp.Handler.RequestHandler
{
    protected override IResourceRequestHandler GetResourceRequestHandler(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling)
    {
        return new ResourceRequestHandler(pubSub, tabId, isChildBrowser);
    }

    protected override bool OnOpenUrlFromTab(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
            return true;

        // When we create background tabs (e.g. via drag/drop of multiple files), Chromium/CefSharp can
        // call this callback during the initial navigation even though nothing is actually trying to
        // open a new window/tab. In those cases we should not cancel default handling. It might be
        // the use of TabsFeature.PreloadTab to preload tabs, that triggers this effect.
        if (targetUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase) && frame.IsMain)
            return false;

        // Only treat this as an actual "open in new tab" request when triggered by a user gesture
        // (or from a non-main frame). Otherwise we risk interfering with tab initialization.
        var isLikelyPopupRequest = userGesture || !frame.IsMain;
        if (!isLikelyPopupRequest)
            return false;

        if (targetDisposition == WindowOpenDisposition.NewBackgroundTab)
        {
            pubSub.Send(new StartNavigationCommand(targetUrl, UseCurrentTab: false, SaveInHistory: true, ActivateTab: false));
            return true;
        }

        return false;
    }
}
