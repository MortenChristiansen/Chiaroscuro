using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.Workspaces;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.Tabs;

public class TabsFeature(MainWindow window) : Feature(window)
{
    private readonly Dictionary<string, List<TabBrowser>> _tabBrowsersByWorkspace = [];
    private string? _currentWorkspaceId;

    public List<TabBrowser>? TabBrowsers => _currentWorkspaceId != null ? _tabBrowsersByWorkspace[_currentWorkspaceId] : null;

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
            Window.SetCurrentTab(TabBrowsers?.Find(t => t.Id == e.TabId))
        );
        PubSub.Subscribe<TabClosedEvent>(e =>
        {
            TabBrowsers?.Remove(e.Tab);
            if (e.Tab == Window.CurrentTab)
                Window.SetCurrentTab(null);
            e.Tab.Dispose();
        });
        PubSub.Subscribe<WorkspaceActivatedEvent>(e =>
        {
            var workspaceFeature = Window.GetFeature<WorkspacesFeature>();
            var workspace = workspaceFeature.GetWorkspaceById(e.WorkspaceId);

            if (!_tabBrowsersByWorkspace.ContainsKey(e.WorkspaceId))
                _tabBrowsersByWorkspace[e.WorkspaceId] = [.. workspace.Tabs.Select(t => AddExistingTab(t.TabId, t.Address, t.IsActive, t.Title, t.Favicon))];
            _currentWorkspaceId = e.WorkspaceId;

            var activeTabId = workspace.Tabs.FirstOrDefault(t => t.IsActive)?.TabId;
            var tabs = workspaceFeature.GetTabsForWorkspace(e.WorkspaceId);
            Window.ActionContext.SetTabs(
                [.. tabs.Select(t => new TabDto(t.TabId, t.Title, t.Favicon, t.Created))],
                activeTabId,
                workspace.EphemeralTabStartIndex,
                [.. workspace.Folders.Select(f => new FolderDto(f.Id, f.Name, f.StartIndex, f.EndIndex))]
            );
            var activeTabBrowser = _tabBrowsersByWorkspace[e.WorkspaceId].FirstOrDefault(t => t.Id == activeTabId);
            Window.SetCurrentTab(activeTabBrowser);
        });
        PubSub.Subscribe<TabMovedToNewWorkspaceEvent>(e =>
        {
            var tab = GetTabBrowserById(e.TabId);

            _tabBrowsersByWorkspace[e.OldWorkspaceId].Remove(tab);
            _tabBrowsersByWorkspace[e.NewWorkspaceId].Add(tab);
        });
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
        var browser = new TabBrowser($"{Guid.NewGuid()}", address, Window.ActionContext, setManualAddress: saveInHistory);
        TabBrowsers?.Add(browser);

        var tab = new TabDto(browser.Id, browser.Title, null, DateTimeOffset.Now);
        Window.ActionContext.AddTab(tab, activate: true);

        Window.Dispatcher.Invoke(() => Window.SetCurrentTab(browser));

        return browser;
    }

    private TabBrowser AddExistingTab(string id, string address, bool activate, string? title, string? favicon)
    {
        var browser = new TabBrowser(id, address, Window.ActionContext, setManualAddress: false);
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
        if (tab == null) return;

        Window.ActionContext.ToggleTabBookmark(tab.Id);
    }

    public TabBrowser GetTabBrowserById(string tabId) =>
        _tabBrowsersByWorkspace.SelectMany(ws => ws.Value).FirstOrDefault(t => t.Id == tabId) ?? throw new ArgumentException("Tab does not exist");
}
