using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public class PinnedTabsBackendApi(PubSub pubSub) : BackendApi
{
    public void UnpinTab(string tabId) =>
        pubSub.Send(new UnpinTabCommand(tabId));

    public void ActivateTab(string tabId) =>
        pubSub.Send(new ActivateTabCommand(tabId));
}
