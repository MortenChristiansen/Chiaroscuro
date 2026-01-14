using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.ActionContext.Tabs;

public class TabsFeature(PubSub pubSub, IBrowserContext browserContext, TabsBrowserApi tabsApi) : Feature(pubSub)
{
    private readonly List<ITabBrowser> _tabBrowsers = [];
    private readonly HashSet<string> _loadedWorkspaceIds = [];

    public override void Configure()
    {
        PubSub.Handle<StartNavigationCommand>(cmd =>
        {
            if (browserContext.CurrentTab != null && cmd.UseCurrentTab)
            {
                browserContext.CurrentTab.SetAddress(cmd.Address, setManualAddress: cmd.SaveInHistory);
            }
            else
            {
                AddNewTab(cmd.Address, cmd.SaveInHistory, cmd.ActivateTab, cmd.ReuseTabBrowser);
            }

            PubSub.Publish(new NavigationStartedEvent(cmd.Address, cmd.UseCurrentTab, cmd.SaveInHistory, cmd.ActivateTab, cmd.ReuseTabBrowser));
        });
        PubSub.Handle<ActivateTabCommand>(cmd =>
        {
            var previousTab = browserContext.CurrentTab;
            SetCurrentTab(GetTabBrowserById(cmd.TabId));
            tabsApi.SetActiveTab(cmd.TabId);
            PubSub.Publish(new TabActivatedEvent(cmd.TabId, previousTab));
        });
        PubSub.Handle<CloseTabCommand>(cmd =>
        {
            var tab = GetTabBrowserById(cmd.TabId);
            _tabBrowsers.Remove(tab);
            if (tab == browserContext.CurrentTab)
                SetCurrentTab(null);
            tab.Dispose();
            browserContext.TabsHost.RemovePreloadedTab(tab.Id);
            PubSub.Publish(new TabClosedEvent(cmd.TabId, tab));
        });

        PubSub.Subscribe<WorkspaceActivatedEvent>(e =>
        {
            var workspaceFeature = browserContext.GetFeature<WorkspacesFeature>();
            var pinnedTabsFeature = browserContext.GetFeature<PinnedTabsFeature>();
            var workspace = workspaceFeature.GetWorkspaceById(e.WorkspaceId);

            if (!_loadedWorkspaceIds.Contains(e.WorkspaceId))
            {
                _tabBrowsers.AddRange(workspace.Tabs.Select(t => AddExistingTab(t.TabId, t.Address, t.Title, t.Favicon)));
                _loadedWorkspaceIds.Add(e.WorkspaceId);
            }

            var activeTabId = pinnedTabsFeature.GetActivePinnedTabId() ?? workspace.Tabs.FirstOrDefault(t => t.IsActive)?.TabId;
            var tabs = workspaceFeature.GetTabsForWorkspace(e.WorkspaceId);
            tabsApi.SetTabs(
                [.. tabs.Select(t => new TabDto(t.TabId, t.Title, t.Favicon, t.Created))],
                activeTabId,
                workspace.EphemeralTabStartIndex,
                [.. workspace.Folders.Select(f => new FolderDto(f.Id, f.Name, f.StartIndex, f.EndIndex))]
            );
            var activeTabBrowser = activeTabId != null ? GetTabBrowserById(activeTabId) : null;
            SetCurrentTab(activeTabBrowser);
        });
    }

    private void SetCurrentTab(ITabBrowser? tab)
    {
        if (browserContext.CurrentTab != null && tab?.Id != browserContext.CurrentTab.Id)
            PubSub.Publish(new TabDeactivatedEvent(browserContext.CurrentTab.Id));

        if (tab != null)
            browserContext.TabsHost.RemovePreloadedTab(tab.Id);

        browserContext.SetCurrentTab(tab);
    }

    public override void Start()
    {
        LoadPinnedTabs();
    }

    private void LoadPinnedTabs()
    {
        var tabs = browserContext.GetFeature<PinnedTabsFeature>().GetPinnedTabs();
        _tabBrowsers.AddRange(tabs.Select(t => AddExistingTab(t.Id, t.Address, t.Title, t.Favicon)));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.B && browserContext.CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            ToggleCurrentTabBookmark();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private ITabBrowser AddNewTab(string address, bool saveInHistory, bool activateTab, ITabBrowser? reuseTabBrowser)
    {
        var browser = reuseTabBrowser ?? browserContext.CreateNewTab(address, tabsApi, PubSub, setManualAddress: saveInHistory, favicon: null, isChildBrowser: false);
        _tabBrowsers.Add(browser);

        var tab = new TabDto(browser.Id, browser.Title, browser.Favicon, DateTimeOffset.UtcNow);
        tabsApi.AddTab(tab, activate: activateTab);

        if (activateTab)
            SetCurrentTab(browser);
        else
            PreloadTab(browser);

        PubSub.Publish(new TabBrowserCreatedEvent(browser));
        return browser;
    }

    private void PreloadTab(ITabBrowser browser)
    {
        browserContext.TabsHost.PreloadTab(browser);
    }

    private ITabBrowser AddExistingTab(string id, string address, string? title, string? favicon)
    {
        var browser = browserContext.CreateExistingTab(id, address, tabsApi, PubSub, setManualAddress: false, favicon: favicon, isChildBrowser: false);
        if (!string.IsNullOrEmpty(title))
            browser.Title = title;

        browser.SavePersistableState();

        PubSub.Publish(new TabBrowserCreatedEvent(browser));
        return browser;
    }

    private void ToggleCurrentTabBookmark()
    {
        var tab = browserContext.CurrentTab;
        if (tab == null || browserContext.GetFeature<PinnedTabsFeature>().IsTabPinned(tab.Id)) return;

        tabsApi.ToggleTabBookmark(tab.Id);
        tab.SavePersistableState();
    }

    public ITabBrowser GetTabBrowserById(string tabId) =>
        _tabBrowsers.FirstOrDefault(t => t.Id == tabId) ?? throw new ArgumentException("Tab does not exist");
}
