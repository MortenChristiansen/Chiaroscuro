using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public record TabPinnedEvent(string TabId);
public record TabUnpinnedEvent(string TabId);

public class PinnedTabsBackendApi(PubSub pubSub) : BackendApi
{
    public void UnpinTab(string tabId) =>
        pubSub.Publish(new TabUnpinnedEvent(tabId));

    public void ActivateTab(string tabId) =>
        pubSub.Publish(new TabActivatedEvent(tabId, MainWindow.Instance.CurrentTab));
}
