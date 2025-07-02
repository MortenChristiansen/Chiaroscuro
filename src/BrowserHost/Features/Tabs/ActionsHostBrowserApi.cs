using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Tabs;

public record TabActivatedEvent(string TabId, TabBrowser? CurrentTab);
public record TabClosedEvent(TabBrowser Tab);
public record TabPositionChangedEvent(string TabId, int NewIndex);
public record TabsChangedEvent(TabUiStateDto[] Tabs);

public record DownloadProgressChangedEvent(DownloadItemDto[] Downloads);
public record DownloadCancelledEvent(string DownloadId);

public record DownloadItemDto(string Id, string FileName, int Progress, bool IsCompleted, bool IsCancelled);

public record TabUiStateDto(string Id, string Title, string? Favicon, bool IsActive);

public class ActionsHostBrowserApi(ActionsHostBrowser browser) : BrowserApi(browser)
{
    public void ActivateTab(string tabId) =>
        PubSub.Publish(new TabActivatedEvent(tabId, MainWindow.Instance.CurrentTab));

    public void CloseTab(string tabId) =>
        PubSub.Publish(new TabClosedEvent(MainWindow.Instance.GetFeature<TabsFeature>().GetTabById(tabId) ?? throw new ArgumentException("Tab does not exist")));

    public void ReorderTab(string tabId, int newIndex) =>
        PubSub.Publish(new TabPositionChangedEvent(tabId, newIndex));

    public void TabsChanged(List<object> tabs) =>
        PubSub.Publish(new TabsChangedEvent(
            [.. tabs.Select((dynamic tab) => new TabUiStateDto(tab.Id, tab.Title, tab.Favicon, tab.IsActive))]
        ));

    public void CancelDownload(string downloadId) =>
        PubSub.Publish(new DownloadCancelledEvent(downloadId));

    public void DownloadsChanged(List<object> downloads) =>
        PubSub.Publish(new DownloadProgressChangedEvent(
            [.. downloads.Select((dynamic download) => new DownloadItemDto(download.Id, download.FileName, download.Progress, download.IsCompleted, download.IsCancelled))]
        ));
}
