using BrowserHost.CefInfrastructure;
using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.PinnedTabs;
public record TabPinnedEvent(string TabId);
public record TabUnpinnedEvent(string TabId);

public class PinnedTabsBrowserApi : BrowserApi
{
    public void UnpinTab(string tabId) =>
        PubSub.Publish(new TabUnpinnedEvent(tabId));

    public void ActivateTab(string tabId) =>
        PubSub.Publish(new TabActivatedEvent(tabId, MainWindow.Instance.CurrentTab));
}
