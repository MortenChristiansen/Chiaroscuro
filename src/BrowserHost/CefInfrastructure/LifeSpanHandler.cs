using CefSharp;
using System;

namespace BrowserHost.CefInfrastructure;

public class LifeSpanHandler : ILifeSpanHandler
{
    private readonly Action<string> _onNewTabRequested;

    public LifeSpanHandler(Action<string> onNewTabRequested)
    {
        _onNewTabRequested = onNewTabRequested;
    }

    public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
    {
        newBrowser = null!;

        // Check if this is a middle mouse click (which would open in a new tab)
        if (targetDisposition == WindowOpenDisposition.NewBackgroundTab || 
            targetDisposition == WindowOpenDisposition.NewForegroundTab)
        {
            // Request a new tab to be created
            _onNewTabRequested(targetUrl);
            
            // Return true to suppress the default popup behavior
            return true;
        }

        // Allow default behavior for other cases
        return false;
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // Nothing to do here
    }

    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        return false;
    }

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // Nothing to do here
    }
}