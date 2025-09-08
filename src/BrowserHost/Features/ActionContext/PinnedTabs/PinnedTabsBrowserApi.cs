using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;
public record TabPinnedEvent(string TabId);
public record TabUnpinnedEvent(string TabId);

public class PinnedTabsBrowserApi : BrowserApi
{
    public void UnpinTab(string tabId) =>
        PubSub.Publish(new TabUnpinnedEvent(tabId));

    public void ActivateTab(string tabId) =>
        PubSub.Publish(new TabActivatedEvent(tabId, MainWindow.Instance.CurrentTab));

    public void ReturnToOriginalAddress(string tabId) =>
        MainWindow.Instance.GetFeature<PinnedTabsFeature>().ReturnToOriginalAddress(tabId);
}
