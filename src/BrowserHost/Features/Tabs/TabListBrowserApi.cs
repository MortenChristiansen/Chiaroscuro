using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Tabs;

public record TabActivatedEvent(string TabId, TabBrowser? CurrentTab);
public record TabClosedEvent(TabBrowser Tab);
public record TabsChangedEvent(TabUiStateDto[] Tabs, int EphemeralTabStartIndex, FolderUiStateDto[] Folders);
public record TabUrlLoadedSuccessfullyEvent(string TabId);
public record TabFaviconUrlChangedEvent(string TabId, string? NewFaviconUrl);
public record FolderNameUpdatedEvent(string FolderId, string NewName);

public record TabUiStateDto(string Id, string Title, string? Favicon, bool IsActive, DateTimeOffset Created);
public record FolderUiStateDto(string Id, string Name, int StartIndex, int EndIndex);

public class TabListBrowserApi : BrowserApi
{
    public void ActivateTab(string tabId) =>
        PubSub.Publish(new TabActivatedEvent(tabId, MainWindow.Instance.CurrentTab));

    public void CloseTab(string tabId) =>
        PubSub.Publish(new TabClosedEvent(MainWindow.Instance.GetFeature<TabsFeature>().GetTabById(tabId) ?? throw new ArgumentException("Tab does not exist")));

    public void TabsChanged(List<object> tabs, int ephemeralTabStartIndex, List<object> folders) =>
        PubSub.Publish(new TabsChangedEvent(
            [.. tabs.Select((dynamic tab) => new TabUiStateDto(tab.Id, tab.Title, tab.Favicon, tab.IsActive, DateTimeOffset.Parse(tab.Created)))],
            ephemeralTabStartIndex,
            [.. folders.Select((dynamic folder) => new FolderUiStateDto(folder.Id, folder.Name, folder.StartIndex, folder.EndIndex))]
        ));
}
