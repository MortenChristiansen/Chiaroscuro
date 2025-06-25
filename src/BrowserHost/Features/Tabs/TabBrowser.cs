using BrowserHost.CefInfrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : BaseBrowser
{
    private readonly TabListBrowser _tabListBrowser;

    public string Id { get; } = $"{Guid.NewGuid()}";

    public TabBrowser(BrowserApi api, string address, TabListBrowser tabListBrowser)
    {
        Address = address;

        TitleChanged += OnTitleChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _tabListBrowser = tabListBrowser;
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _tabListBrowser.UpdateTab(new TabDto(Id, (string)e.NewValue, null));
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Dispatcher.BeginInvoke(() => _tabListBrowser.UpdateTab(new TabDto(Id, Title, addresses.FirstOrDefault())));
    }
}
