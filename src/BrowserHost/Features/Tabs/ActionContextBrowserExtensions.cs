using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.Tabs;
public static class ActionContextBrowserExtensions
{
    public static void AddTab(this ActionContextBrowser browser, TabDto tab, bool activate = true)
    {
        browser.RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.addTab({tab.ToJsonObject()}, {activate.ToJsonBoolean()})";
            browser.ExecuteScriptAsync(script);
        });
    }

    public static void SetTabs(this ActionContextBrowser browser, TabDto[] tabs, string? activeTabId)
    {
        browser.RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.setTabs({tabs.ToJsonObject()}, {activeTabId.ToJsonString()})";
            browser.ExecuteScriptAsync(script);
        });
    }

    public static void UpdateTabTitle(this ActionContextBrowser browser, string tabId, string? title)
    {
        browser.RunWhenSourceHasLoaded(() =>
        {
            browser.ExecuteScriptAsync($"window.angularApi.updateTitle({tabId.ToJsonString()}, {title.ToJsonString()})");
        });
    }

    public static void UpdateTabFavicon(this ActionContextBrowser browser, string tabId, string? favicon)
    {
        browser.RunWhenSourceHasLoaded(() =>
        {
            browser.ExecuteScriptAsync($"window.angularApi.updateFavicon({tabId.ToJsonString()}, {favicon.ToJsonString()})");
        });
    }

    public static void CloseTab(this ActionContextBrowser browser, string tabId)
    {
        browser.RunWhenSourceHasLoaded(() =>
        {
            browser.ExecuteScriptAsync($"window.angularApi.closeTab({tabId.ToJsonString()})");
        });
    }
}
