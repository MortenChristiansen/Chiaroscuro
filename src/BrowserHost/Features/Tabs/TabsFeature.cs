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
            if (!_tabBrowsersByWorkspace.ContainsKey(e.WorkspaceId))
                _tabBrowsersByWorkspace[e.WorkspaceId] = [.. e.Workspace.Tabs.Select(t => AddExistingTab(t.TabId, t.Address, t.IsActive, t.Title, t.Favicon))];
            _currentWorkspaceId = e.WorkspaceId;

            var activeTabId = e.Workspace.Tabs.FirstOrDefault(t => t.IsActive)?.TabId;
            var activeTabBrowser = _tabBrowsersByWorkspace[e.WorkspaceId].FirstOrDefault(t => t.Id == activeTabId);
            Window.SetCurrentTab(activeTabBrowser);
        });
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

    public TabBrowser? GetTabById(string tabId) =>
        TabBrowsers?.Find(t => t.Id == tabId);
}
