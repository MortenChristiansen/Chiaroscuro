using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.Tabs;

public class TabListBrowser : Browser<TabListBrowserApi>
{
    public override TabListBrowserApi Api { get; }

    public TabListBrowser()
        : base("/tabs")
    {
        Api = new TabListBrowserApi(this);
    }

    public void AddTab(TabDto tab, bool activate = true)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.addTab({tab.ToJsonObject()}, {activate.ToJsonBoolean()})";
            this.ExecuteScriptAsync(script);
        });
    }

    public void UpdateTabTitle(string tabId, string? title)
    {
        RunWhenSourceHasLoaded(() =>
        {
            this.ExecuteScriptAsync($"window.angularApi.updateTitle({tabId.ToJsonString()}, {title.ToJsonString()})");
        });
    }

    public void UpdateTabFavicon(string tabId, string? favicon)
    {
        RunWhenSourceHasLoaded(() =>
        {
            this.ExecuteScriptAsync($"window.angularApi.updateFavicon({tabId.ToJsonString()}, {favicon.ToJsonString()})");
        });
    }

    public void CloseTab(string tabId, string? focusedTabId)
    {
        RunWhenSourceHasLoaded(() =>
        {
            this.ExecuteScriptAsync($"window.angularApi.closeTab({tabId.ToJsonString()}, {focusedTabId.ToJsonString()})");
        });
    }
}

