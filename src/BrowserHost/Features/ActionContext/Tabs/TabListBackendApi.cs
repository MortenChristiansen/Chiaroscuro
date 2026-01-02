using BrowserHost.CefInfrastructure;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.ActionContext.Tabs;

public record ActivateTabCommand(string TabId) : ICommand;
public record CloseTabCommand(string TabId) : ICommand;
public record ChangeTabsCommand(TabUiStateDto[] Tabs, int EphemeralTabStartIndex, FolderUiStateDto[] Folders) : ICommand;

public record TabActivatedEvent(string TabId, TabBrowser? PreviousTab) : IEvent;
public record TabDeactivatedEvent(string TabId) : IEvent;
public record TabClosedEvent(string TabId, TabBrowser Tab) : IEvent;
public record TabsChangedEvent(TabUiStateDto[] Tabs, int EphemeralTabStartIndex, FolderUiStateDto[] Folders) : IEvent;
public record TabUrlLoadedSuccessfullyEvent(string TabId) : IEvent;
public record TabFaviconUrlChangedEvent(string TabId, string? NewFaviconUrl) : IEvent;
public record TabBrowserCreatedEvent(TabBrowser TabBrowser) : IEvent;

public record TabUiStateDto(string Id, string Title, string? Favicon, bool IsActive, DateTimeOffset Created);
public record FolderUiStateDto(string Id, string Name, int StartIndex, int EndIndex);

public class TabListBackendApi(PubSub pubSub) : BackendApi
{
    public void ActivateTab(string tabId) =>
        pubSub.Send(new ActivateTabCommand(tabId));

    public void CloseTab(string tabId) =>
        pubSub.Send(new CloseTabCommand(tabId));

    public void TabsChanged(List<object> tabs, int ephemeralTabStartIndex, List<object> folders) =>
        pubSub.Send(new ChangeTabsCommand(
            [.. tabs.Select((dynamic tab) => new TabUiStateDto(tab.Id, tab.Title, tab.Favicon, tab.IsActive, DateTimeOffset.Parse(tab.Created)))],
            ephemeralTabStartIndex,
            [.. folders.Select((dynamic folder) => new FolderUiStateDto(folder.Id, folder.Name, folder.StartIndex, folder.EndIndex))]
        ));
}
