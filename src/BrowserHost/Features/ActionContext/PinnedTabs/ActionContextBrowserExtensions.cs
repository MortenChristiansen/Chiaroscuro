using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public record PinnedTabDto(string Id, string? Title, string? Favicon);

public static class ActionContextBrowserExtensions
{
    public static void SetPinnedTabs(this ActionContextBrowser browser, PinnedTabDto[] tabs, string? activeTabId)
    {
        browser.CallClientApi("setPinnedTabs", $"{tabs.ToJsonObject()}, {activeTabId.ToJsonString()}");
    }
}
