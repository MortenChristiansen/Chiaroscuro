using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.PinnedTabs;

public record PinnedTabUnpinnedEvent(string TabId);
public record ActivatePinnedTabEvent(string TabId); // Use this or the existing event?

public class PinnedTabsBrowserApi : BrowserApi
{
    public void UnpinTab(string tabId) =>
        PubSub.Publish(new PinnedTabUnpinnedEvent(tabId));

    public void ActivateTab(string tabId) =>
        PubSub.Publish(new ActivatePinnedTabEvent(tabId));
}
