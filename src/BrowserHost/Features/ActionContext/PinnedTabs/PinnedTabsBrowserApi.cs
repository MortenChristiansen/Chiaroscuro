using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public record PinnedTabDto(string Id, string? Title, string? Favicon);

public class PinnedTabsBrowserApi(BaseBrowser actionContextBrowser) : BrowserApi(actionContextBrowser)
{
    public void SetPinnedTabs(PinnedTabDto[] tabs, string? activeTabId) =>
        CallClientApi("setPinnedTabs", $"{tabs.ToJsonObject()}, {activeTabId.ToJsonString()}");
}
