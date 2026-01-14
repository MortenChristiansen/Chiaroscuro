using BrowserHost.CefInfrastructure;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.ActionContext.Tabs;

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
