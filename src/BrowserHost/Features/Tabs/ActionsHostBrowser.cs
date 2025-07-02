using BrowserHost.CefInfrastructure;
using BrowserHost.Features.FileDownload;
using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.Tabs;

public class ActionsHostBrowser : Browser<ActionsHostBrowserApi>
{
    public override ActionsHostBrowserApi Api { get; }

    public ActionsHostBrowser()
        : base("/actions-host")
    {
        Api = new ActionsHostBrowserApi(this);
    }

    public void AddTab(TabDto tab, bool activate = true)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.addTab({tab.ToJsonObject()}, {activate.ToJsonBoolean()})";
            this.ExecuteScriptAsync(script);
        });
    }

    public void SetTabs(TabDto[] tabs, string? activeTabId)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.setTabs({tabs.ToJsonObject()}, {activeTabId.ToJsonString()})";
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

    public void CloseTab(string tabId)
    {
        RunWhenSourceHasLoaded(() =>
        {
            this.ExecuteScriptAsync($"window.angularApi.closeTab({tabId.ToJsonString()})");
        });
    }

    public void UpdateDownloads(DownloadItemDto[] downloads)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.downloadsChanged({downloads.ToJsonObject()})";
            this.ExecuteScriptAsync(script);
        });
    }
}

