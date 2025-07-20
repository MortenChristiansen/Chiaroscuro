using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.Workspaces;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.Tabs;

public class TabsFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.ActionContext.TabListApi)
{
    private readonly List<TabBrowser> _tabBrowsers = [];
    private WorkspacesFeature? _workspacesFeature;

    public override void Register()
    {
        // We'll initialize workspace-aware tab restoration after workspaces feature is ready
        _workspacesFeature = Window.GetFeature<WorkspacesFeature>();
        RestoreWorkspaceTabs();

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
            Window.SetCurrentTab(_tabBrowsers.Find(t => t.Id == e.TabId))
        );
        PubSub.Subscribe<TabClosedEvent>(e =>
        {
            _tabBrowsers.Remove(e.Tab);
            if (e.Tab == Window.CurrentTab)
                Window.SetCurrentTab(null);
            e.Tab.Dispose();
        });
        PubSub.Subscribe<TabsChangedEvent>(e =>
        {
            var currentWorkspaceId = _workspacesFeature?.GetActiveWorkspaceId();
            if (currentWorkspaceId != null)
            {
                var tabs = e.Tabs.Select(t => new TabStateDtoV1(_tabBrowsers.Find(b => b.Id == t.Id)?.Address ?? "", t.Title, t.Favicon, t.IsActive, t.Created)).ToArray();
                var activeTabId = e.Tabs.FirstOrDefault(t => t.IsActive)?.Id;
                _workspacesFeature.UpdateWorkspaceTabs(currentWorkspaceId, tabs, activeTabId);
            }
        });
    }

    private void RestoreWorkspaceTabs()
    {
        var workspacesData = _workspacesFeature?.GetWorkspacesData();
        if (workspacesData != null)
        {
            var activeWorkspace = workspacesData.Workspaces.FirstOrDefault(w => w.Id == workspacesData.ActiveWorkspaceId);
            if (activeWorkspace != null)
            {
                LoadWorkspaceTabs(activeWorkspace.Tabs, activeWorkspace.LastActiveTabId);
            }
        }
        else
        {
            // Fallback to legacy tab restoration if workspaces aren't available
            RestoreTabs();
        }
    }

    private void RestoreTabs()
    {
        var tabs = TabStateManager.RestoreTabsFromDisk();
        var browsers = tabs.Tabs.Select(t => (Browser: AddExistingTab(t.Address, activate: t.IsActive, t.Title, t.Favicon), Tab: t)).ToList();
        Window.ActionContext.SetTabs(
            [.. browsers.Select(t => new TabDto(t.Browser.Id, t.Tab.Title, t.Tab.Favicon, t.Tab.Created))],
            browsers.Find(t => t.Tab.IsActive).Browser?.Id,
            tabs.EphemeralTabStartIndex
        );
    }

    public void LoadWorkspaceTabs(TabStateDtoV1[] workspaceTabs, string? lastActiveTabId)
    {
        // Clear existing tabs
        foreach (var browser in _tabBrowsers.ToList())
        {
            browser.Dispose();
        }
        _tabBrowsers.Clear();

        // Load workspace tabs
        var browsers = workspaceTabs.Select(t => AddExistingTab(t.Address, activate: false, t.Title, t.Favicon)).ToList();
        
        // Find the last active tab or pick the first ephemeral one
        string? activeTabId = null;
        if (!string.IsNullOrEmpty(lastActiveTabId))
        {
            activeTabId = browsers.FirstOrDefault(b => _tabBrowsers.Any(tb => tb.Id == b.Id && tb.Address == lastActiveTabId))?.Id;
        }
        
        // If no last active tab found, activate the topmost ephemeral tab
        if (activeTabId == null && browsers.Any())
        {
            // For now, assume ephemeral tabs start from the end - this logic can be refined
            var ephemeralTabs = browsers.Where(b => !workspaceTabs.Any(t => t.Address == _tabBrowsers.First(tb => tb.Id == b.Id).Address)).ToList();
            activeTabId = ephemeralTabs.FirstOrDefault()?.Id ?? browsers.FirstOrDefault()?.Id;
        }

        Window.ActionContext.SetTabs(
            [.. browsers.Select(b => new TabDto(b.Id, b.Title, null, DateTimeOffset.Now))],
            activeTabId,
            workspaceTabs.Length // For now, assume all restored tabs are persistent
        );
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.X && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            CloseCurrentTab();
            return true;
        }

        if (e.Key == Key.B && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            ToggleCurrentTabBookmark();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private TabBrowser AddNewTab(string address, bool saveInHistory)
    {
        var browser = new TabBrowser(address, Window.ActionContext, setManualAddress: saveInHistory);
        _tabBrowsers.Add(browser);

        RegisterNewTabWithFrontend(browser);

        return browser;
    }

    private void RegisterNewTabWithFrontend(TabBrowser browser)
    {
        var tab = new TabDto(browser.Id, browser.Title, null, DateTimeOffset.Now);
        Window.ActionContext.AddTab(tab, activate: true);
        Window.Dispatcher.Invoke(() => Window.SetCurrentTab(browser));
    }

    private TabBrowser AddExistingTab(string address, bool activate, string? title, string? favicon)
    {
        var browser = new TabBrowser(address, Window.ActionContext, setManualAddress: false);
        if (!string.IsNullOrEmpty(title))
            browser.Title = title;

        _tabBrowsers.Add(browser);

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

    public TabBrowser? GetTabById(string tabId)
    {
        return _tabBrowsers.FirstOrDefault(t => t.Id == tabId);
    }
}
