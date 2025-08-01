using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.PinnedTabs;
using BrowserHost.Features.Workspaces;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.Tabs;

public class TabsFeature(MainWindow window) : Feature(window)
{
    private readonly List<TabBrowser> _tabBrowsers = [];
    private readonly HashSet<string> _loadedWorkspaceIds = [];

    public override void Configure()
    {
        PubSub.Subscribe<NavigationStartedEvent>(e =>
        {
            if (Window.CurrentTab != null && e.UseCurrentTab)
            {
                Window.CurrentTab.SetAddress(e.Address, setManualAddress: e.SaveInHistory);
            }
            else
            {
                AddNewTab(e.Address, e.SaveInHistory);
            }
        });
        PubSub.Subscribe<TabActivatedEvent>(e =>
        {
            Window.SetCurrentTab(_tabBrowsers.Find(t => t.Id == e.TabId));
            Window.ActionContext.SetActiveTab(e.TabId);
        });
        PubSub.Subscribe<TabClosedEvent>(e =>
        {
            _tabBrowsers.Remove(e.Tab);
            if (e.Tab == Window.CurrentTab)
                Window.SetCurrentTab(null);
            e.Tab.Dispose();
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
            Window.ActionContext.SetTabs(
                [.. tabs.Select(t => new TabDto(t.TabId, t.Title, t.Favicon, t.Created))],
                activeTabId,
                workspace.EphemeralTabStartIndex,
                [.. workspace.Folders.Select(f => new FolderDto(f.Id, f.Name, f.StartIndex, f.EndIndex))]
            );
            var activeTabBrowser = activeTabId != null ? GetTabBrowserById(activeTabId) : null;
            Window.SetCurrentTab(activeTabBrowser);
        });
    }

    public override void Start()
    {
        LoadPinnedTabs();
    }

    private void LoadPinnedTabs()
    {
        var pinedTabsFeature = Window.GetFeature<PinnedTabsFeature>();
        var tabs = pinedTabsFeature.GetPinnedTabs();
        _tabBrowsers.AddRange(tabs.Select(t => AddExistingTab(t.Id, t.Address, t.Title, t.Favicon)));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.X && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            CloseCurrentTab();
            return true;
        }

        if (e.Key == Key.B && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            ToggleCurrentTabBookmark();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private TabBrowser AddNewTab(string address, bool saveInHistory)
    {
        var browser = new TabBrowser($"{Guid.NewGuid()}", address, Window.ActionContext, setManualAddress: saveInHistory, favicon: null);
        _tabBrowsers.Add(browser);

        var tab = new TabDto(browser.Id, browser.Title, null, DateTimeOffset.UtcNow);
        Window.ActionContext.AddTab(tab, activate: true);
        Window.Dispatcher.Invoke(() => Window.SetCurrentTab(browser));

        return browser;
    }

    private TabBrowser AddExistingTab(string id, string address, string? title, string? favicon)
    {
        var browser = new TabBrowser(id, address, Window.ActionContext, setManualAddress: false, favicon);
        if (!string.IsNullOrEmpty(title))
            browser.Title = title;

        return browser;
    }

    private void CloseCurrentTab()
    {
        var tab = Window.CurrentTab;
        if (tab == null) return;

        Window.ActionContext.CloseTab(tab.Id);
        PubSub.Publish(new TabClosedEvent(tab));
    }

    private void ToggleCurrentTabBookmark()
    {
        var tab = Window.CurrentTab;
        if (tab == null || Window.GetFeature<PinnedTabsFeature>().IsTabPinned(tab.Id)) return;

        Window.ActionContext.ToggleTabBookmark(tab.Id);
    }

    public TabBrowser GetTabBrowserById(string tabId) =>
        _tabBrowsers.FirstOrDefault(t => t.Id == tabId) ?? throw new ArgumentException("Tab does not exist");
}
