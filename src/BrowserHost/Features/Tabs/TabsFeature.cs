using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost.Features.Tabs;

public record TabStateDto(string Address, string? Title, string? Favicon, bool IsActive);

public class TabsFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.Tabs.Api)
{
    private readonly List<TabBrowser> _tabBrowsers = [];

    public override void Register()
    {
        var tabs = TabStateManager.RestoreTabsFromDisk();
        var browsers = tabs.Select(t => (Browser: AddTab(t.Address, activate: t.IsActive, t.Title, t.Favicon, addToFrontend: false), Tab: t)).ToList();
        Window.Tabs.SetTabs(
            [.. browsers.Select(t => new TabDto(t.Browser.Id, t.Tab.Title, t.Tab.Favicon))],
            browsers.FirstOrDefault(t => t.Tab.IsActive).Browser?.Id
        );

        _ = Listen(Window.ActionDialog.Api.NavigationStartedChannel, e =>
        {
            if (Window.CurrentTab != null && e.UseCurrentTab)
            {
                Window.ChromeUI.ChangeAddress(e.Address);
                Window.CurrentTab.Address = e.Address;
            }
            else
            {
                AddTab(e.Address, activate: true);
            }
        }, dispatchToUi: true);
        _ = Listen(
            Api.TabActivatedChannel,
            e =>
            {
                var tab = _tabBrowsers.FirstOrDefault(t => t.Id == e.TabId);
                Window.SetCurrentTab(tab);
                Window.ChromeUI.ChangeAddress(tab?.Address);
            },
            dispatchToUi: true
        );
        _ = Listen(Api.TabClosedChannel, e =>
        {
            var tab = _tabBrowsers.FirstOrDefault(t => t.Id == e.TabId);
            if (tab != null)
            {
                _tabBrowsers.Remove(tab);
                if (tab == Window.CurrentTab)
                    Window.SetCurrentTab(FindNextTab(tab));
            }
        }, dispatchToUi: true);
        _ = Listen(Api.TabPositionChangedChannel, e =>
        {
            var tab = _tabBrowsers.FirstOrDefault(t => t.Id == e.TabId);
            if (tab != null)
            {
                _tabBrowsers.Remove(tab);
                _tabBrowsers.Insert(e.NewIndex, tab);
            }
        }, dispatchToUi: true);
        _ = Listen(Api.TabsChangedChannel, e =>
            TabStateManager.SaveTabsToDisk(e.Tabs.Select(t => new TabStateDto(_tabBrowsers.FirstOrDefault(b => b.Id == t.Id)?.Address ?? "", t.Title, t.Favicon, t.IsActive)))
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

    private TabBrowser AddTab(string address, bool activate, string? title = null, string? favicon = null, bool addToFrontend = true)
    {
        var browser = new TabBrowser(Api, address, Window.Tabs);
        if (!string.IsNullOrEmpty(title))
            browser.Title = title;

        browser.AddressChanged += Tab_AddressChanged;
        _tabBrowsers.Add(browser);

        if (addToFrontend)
            RegisterTabWithFrontend(activate, favicon, browser);

        return browser;
    }

    private void RegisterTabWithFrontend(bool activate, string? favicon, TabBrowser browser)
    {
        var tab = new TabDto(browser.Id, browser.Title, favicon);
        Window.Tabs.AddTab(tab, activate);

        if (activate)
            Window.Dispatcher.Invoke(() => Window.SetCurrentTab(browser));
    }

    private void Tab_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender == Window.CurrentTab)
            Window.ChromeUI.ChangeAddress($"{e.NewValue}");
    }

    private void CloseCurrentTab()
    {
        var tab = Window.CurrentTab;
        if (tab == null) return;

        TabBrowser? nextTab = FindNextTab(tab);
        _tabBrowsers.Remove(tab);
        Window.Tabs.CloseTab(tab.Id, nextTab?.Id);
        Window.SetCurrentTab(nextTab);
    }

    private TabBrowser? FindNextTab(TabBrowser tab)
    {
        int index = _tabBrowsers.IndexOf(tab);
        if (index == -1 || _tabBrowsers.Count <= 1)
            return null;
        // Prefer previous tab if possible
        if (index > 0)
            return _tabBrowsers[index - 1];
        // Otherwise, return next tab
        if (index < _tabBrowsers.Count - 1)
            return _tabBrowsers[index + 1];
        return null;
    }
}
