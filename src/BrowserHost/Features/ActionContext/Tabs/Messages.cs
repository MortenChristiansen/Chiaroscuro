using BrowserHost.Tab;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.Tabs;

public record ActivateTabCommand(string TabId) : ICommand;
public record TabActivatedEvent(string TabId, TabBrowser? PreviousTab) : IEvent;

public record CloseTabCommand(string TabId) : ICommand;
public record TabClosedEvent(string TabId, TabBrowser Tab) : IEvent;

public record ChangeTabsCommand(TabUiStateDto[] Tabs, int EphemeralTabStartIndex, FolderUiStateDto[] Folders) : ICommand;
public record TabsChangedEvent(TabUiStateDto[] Tabs, int EphemeralTabStartIndex, FolderUiStateDto[] Folders) : IEvent;

public record TabDeactivatedEvent(string TabId) : IEvent;
public record TabUrlLoadedSuccessfullyEvent(string TabId) : IEvent;
public record TabFaviconUrlChangedEvent(string TabId, string? NewFaviconUrl) : IEvent;
public record TabBrowserCreatedEvent(TabBrowser TabBrowser) : IEvent;
