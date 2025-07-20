using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Tabs;

public class NavigationBrowserApi : BrowserApi
{
    public void OpenLinkInNewTab(string url)
    {
        // Publish event to request a new tab (similar to how other navigation works)
        PubSub.Publish(new NavigationStartedEvent(url, UseCurrentTab: false, SaveInHistory: true));
    }
}