using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost.Features.Tabs;

public class TabsFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.Tabs.Api)
{
    private readonly List<TabBrowser> _tabBrowsers = [];

    public override void Register()
    {
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
                // Is there a more declarative way to do this?
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
        _ = Listen(Api.TabPositionChanged, e =>
        {
            var tab = _tabBrowsers.FirstOrDefault(t => t.Id == e.TabId);
            if (tab != null)
            {
                _tabBrowsers.Remove(tab);
                _tabBrowsers.Insert(e.NewIndex, tab);
            }
        }, dispatchToUi: true);
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

    private void AddTab(string address, bool activate)
    {
        var browser = new TabBrowser(Api, address, Window.Tabs);

        browser.AddressChanged += Tab_AddressChanged;

        AddTab(browser, activate);
    }

    private void AddTab(TabBrowser browser, bool activate)
    {
        _tabBrowsers.Add(browser);

        var tab = new TabDto(browser.Id, browser.Title, null);
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
