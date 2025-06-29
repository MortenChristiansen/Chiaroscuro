using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace BrowserHost.Features.Tabs;

public record TabStateDto(string Address, string? Title, string? Favicon, bool IsActive);

public class TabsFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.Tabs.Api)
{
    private readonly List<TabBrowser> _tabBrowsers = [];

    public override void Register()
    {
        RestoreTabs();

        PubSub.Subscribe<NavigationStartedEvent>(e =>
        {
            if (Window.CurrentTab != null && e.UseCurrentTab)
            {
                Window.CurrentTab.SetManuallyNavigatedAddress(e.Address);
            }
            else
            {
                AddNewTab(e.Address);
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
        });
        PubSub.Subscribe<TabsChangedEvent>(e =>
            TabStateManager.SaveTabsToDisk(e.Tabs.Select(t => new TabStateDto(_tabBrowsers.Find(b => b.Id == t.Id)?.Address ?? "", t.Title, t.Favicon, t.IsActive)))
        );
    }

    private void RestoreTabs()
    {
        var tabs = TabStateManager.RestoreTabsFromDisk();
        var browsers = tabs.Select(t => (Browser: AddExistingTab(t.Address, activate: t.IsActive, t.Title, t.Favicon), Tab: t)).ToList();
        Window.Tabs.SetTabs(
            [.. browsers.Select(t => new TabDto(t.Browser.Id, t.Tab.Title, t.Tab.Favicon))],
            browsers.Find(t => t.Tab.IsActive).Browser?.Id
        );
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.X && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            CloseCurrentTab();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private TabBrowser AddNewTab(string address)
    {
        var browser = new TabBrowser(address, Window.Tabs, isNewTab: true);
        _tabBrowsers.Add(browser);

        RegisterNewTabWithFrontend(browser);

        return browser;
    }

    private void RegisterNewTabWithFrontend(TabBrowser browser)
    {
        var tab = new TabDto(browser.Id, browser.Title, null);
        Window.Tabs.AddTab(tab, activate: true);
        Window.Dispatcher.Invoke(() => Window.SetCurrentTab(browser));
    }

    private TabBrowser AddExistingTab(string address, bool activate, string? title, string? favicon)
    {
        var browser = new TabBrowser(address, Window.Tabs, isNewTab: false);
        if (!string.IsNullOrEmpty(title))
            browser.Title = title;

        _tabBrowsers.Add(browser);

        return browser;
    }

    private void CloseCurrentTab()
    {
        var tab = Window.CurrentTab;
        if (tab == null) return;

        Window.Tabs.CloseTab(tab.Id);
        PubSub.Publish(new TabClosedEvent(tab));
    }

    public TabBrowser? GetTabById(string tabId)
    {
        return _tabBrowsers.FirstOrDefault(t => t.Id == tabId);
    }
}
