using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Tabs;
public static class ActionContextBrowserExtensions
{
    public static void AddTab(this ActionContextBrowser browser, TabDto tab, bool activate = true)
    {
        browser.CallClientApi("addTab", $"{tab.ToJsonObject()}, {activate.ToJsonBoolean()}");
    }

    public static void SetTabs(this ActionContextBrowser browser, TabDto[] tabs, string? activeTabId)
    {
        browser.CallClientApi("setTabs", $"{tabs.ToJsonObject()}, {activeTabId.ToJsonString()}");
    }

    public static void UpdateTabTitle(this ActionContextBrowser browser, string tabId, string? title)
    {
        browser.CallClientApi("updateTitle", $"{tabId.ToJsonString()}, {title.ToJsonString()}");
    }

    public static void UpdateTabFavicon(this ActionContextBrowser browser, string tabId, string? favicon)
    {
        browser.CallClientApi("updateFavicon", $"{tabId.ToJsonString()}, {favicon.ToJsonString()}");
    }

    public static void CloseTab(this ActionContextBrowser browser, string tabId)
    {
        browser.CallClientApi("closeTab", tabId.ToJsonString());
    }
}
