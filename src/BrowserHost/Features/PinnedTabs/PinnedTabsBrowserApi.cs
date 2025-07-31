using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.PinnedTabs;

public record PinnedTabUnpinnedEvent(string TabId);

public class PinnedTabsBrowserApi : BrowserApi
{
    public void UnpinTab(string tabId)
    {
        PubSub.Publish(new PinnedTabUnpinnedEvent(tabId));
    }
}
