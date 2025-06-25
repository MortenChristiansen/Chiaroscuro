using System.Collections.Generic;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabsFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.Tabs.Api)
{
    private readonly Dictionary<string, TabBrowser> _tabBrowsers = [];

    public override void Register()
    {
        _ = Listen(Window.ActionDialog.Api.NavigationStartedChannel, e => AddTab(e.Address, activate: true), dispatchToUi: true);
    }

    private void AddTab(string address, bool activate)
    {
        var browser = new TabBrowser(Api, address, Window.Tabs);

        browser.AddressChanged += Tab_AddressChanged;

        AddTab(browser, activate);
    }

    private void AddTab(TabBrowser browser, bool activate)
    {
        _tabBrowsers[browser.Id] = browser;

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
}
