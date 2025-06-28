using BrowserHost.CefInfrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : Browser
{
    private readonly TabListBrowser _tabListBrowser;

    public string Id { get; } = $"{Guid.NewGuid()}";
    public string? Favicon { get; private set; }

    public event EventHandler? FaviconChanged;

    public TabBrowser(BrowserApi api, string address, TabListBrowser tabListBrowser)
    {
        Address = address;

        TitleChanged += OnTitleChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _tabListBrowser = tabListBrowser;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _tabListBrowser.UpdateTabTitle(Id, (string)e.NewValue);
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        Dispatcher.BeginInvoke(() => _tabListBrowser.UpdateTabFavicon(Id, Favicon));
        FaviconChanged?.Invoke(this, EventArgs.Empty);
    }
}
