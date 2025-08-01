using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.PinnedTabs;

public record ActivatePinnedTabEvent(string TabId); // Use this or the existing event?
public record TabPinnedEvent(string TabId);
public record TabUnpinnedEvent(string TabId);

public class PinnedTabsBrowserApi : BrowserApi
{
    public void UnpinTab(string tabId) =>
        PubSub.Publish(new TabUnpinnedEvent(tabId));

    public void ActivateTab(string tabId) =>
        PubSub.Publish(new ActivatePinnedTabEvent(tabId));
}
