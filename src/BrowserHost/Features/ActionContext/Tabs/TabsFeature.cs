using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace BrowserHost.Features.ActionContext.Tabs;

public class TabsFeature(MainWindow window, PubSub pubSub, TabsBrowserApi tabsApi) : Feature(window, pubSub)
{
    private readonly List<TabBrowser> _tabBrowsers = [];
    private readonly HashSet<string> _loadedWorkspaceIds = [];

    public override void Configure()
    {
        PubSub.Handle<StartNavigationCommand>(cmd =>
        {
            if (Window.CurrentTab != null && cmd.UseCurrentTab)
            {
                Window.CurrentTab.SetAddress(cmd.Address, setManualAddress: cmd.SaveInHistory);
            }
            else
            {
                AddNewTab(cmd.Address, cmd.SaveInHistory, cmd.ActivateTab, cmd.ReuseTabBrowser);
            }

            PubSub.Publish(new NavigationStartedEvent(cmd.Address, cmd.UseCurrentTab, cmd.SaveInHistory, cmd.ActivateTab, cmd.ReuseTabBrowser));
        });
        PubSub.Handle<ActivateTabCommand>(cmd =>
        {
            var previousTab = Window.CurrentTab;
            SetCurrentTab(_tabBrowsers.Find(t => t.Id == cmd.TabId));
            tabsApi.SetActiveTab(cmd.TabId);
            PubSub.Publish(new TabActivatedEvent(cmd.TabId, previousTab));
        });
        PubSub.Handle<CloseTabCommand>(cmd =>
        {
            var tab = GetTabBrowserById(cmd.TabId);
            _tabBrowsers.Remove(tab);
            if (tab == Window.CurrentTab)
                SetCurrentTab(null);
            TryRemoveFromPreloadHost(tab);
            tab.Dispose();
            PubSub.Publish(new TabClosedEvent(cmd.TabId, tab));
        });

        PubSub.Subscribe<WorkspaceActivatedEvent>(e =>
        {
            var workspaceFeature = Window.GetFeature<WorkspacesFeature>();
            var pinnedTabsFeature = Window.GetFeature<PinnedTabsFeature>();
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

    private void SetCurrentTab(TabBrowser? tab)
    {
        if (Window.CurrentTab != null && tab?.Id != Window.CurrentTab.Id)
            PubSub.Publish(new TabDeactivatedEvent(Window.CurrentTab.Id));

        if (tab != null)
            TryRemoveFromPreloadHost(tab);

        Window.SetCurrentTab(tab);
    }

    public override void Start()
    {
        LoadPinnedTabs();
    }

    private void LoadPinnedTabs()
    {
        var tabs = Window.GetFeature<PinnedTabsFeature>().GetPinnedTabs();
        _tabBrowsers.AddRange(tabs.Select(t => AddExistingTab(t.Id, t.Address, t.Title, t.Favicon)));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ToggleCurrentTabBookmark();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private TabBrowser AddNewTab(string address, bool saveInHistory, bool activateTab, TabBrowser? reuseTabBrowser)
    {
        var browser = reuseTabBrowser ?? new TabBrowser($"{Guid.NewGuid()}", address, tabsApi, PubSub, setManualAddress: saveInHistory, favicon: null, isChildBrowser: false);
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

    private void PreloadTab(TabBrowser browser)
    {
        var host = (Grid)Window.FindName("PreloadTabsHost");
        if (host.Children.OfType<TabBrowser>().Any(tb => tb.Id == browser.Id)) return;
        host.Children.Add(browser);
    }

    private void TryRemoveFromPreloadHost(TabBrowser browser)
    {
        var host = (Grid)Window.FindName("PreloadTabsHost");
        foreach (var child in host.Children.OfType<TabBrowser>().Where(tb => tb.Id == browser.Id).ToArray())
            host.Children.Remove(child);
    }

    private TabBrowser AddExistingTab(string id, string address, string? title, string? favicon)
    {
        var browser = new TabBrowser(id, address, tabsApi, PubSub, setManualAddress: false, favicon: favicon, isChildBrowser: false);
        if (!string.IsNullOrEmpty(title))
            browser.Title = title;

        browser.SavePersistableState();

        PubSub.Publish(new TabBrowserCreatedEvent(browser));
        return browser;
    }

    private void ToggleCurrentTabBookmark()
    {
        var tab = Window.CurrentTab;
        if (tab == null || Window.GetFeature<PinnedTabsFeature>().IsTabPinned(tab.Id)) return;

        tabsApi.ToggleTabBookmark(tab.Id);
        tab.SavePersistableState();
    }

    public TabBrowser GetTabBrowserById(string tabId) =>
        _tabBrowsers.FirstOrDefault(t => t.Id == tabId) ?? throw new ArgumentException("Tab does not exist");
}
