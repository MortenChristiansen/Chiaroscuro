using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.FileDownloads;
using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : Browser
{
    private readonly ActionContextBrowser _actionContextBrowser;

    public string Id { get; } = $"{Guid.NewGuid()}";
    public string? Favicon { get; private set; }
    public string? ManualAddress { get; private set; }

    private event EventHandler? FaviconChanged;

    public TabBrowser(string address, ActionContextBrowser actionContextBrowser, bool isNewTab)
    {
        Address = address;
        if (isNewTab)
            ManualAddress = address;

        TitleChanged += OnTitleChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _actionContextBrowser = actionContextBrowser;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _actionContextBrowser.UpdateTabTitle(Id, (string)e.NewValue);
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        Dispatcher.BeginInvoke(() => _actionContextBrowser.UpdateTabFavicon(Id, Favicon));
        FaviconChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetManuallyNavigatedAddress(string address)
    {
        Address = address;
        ManualAddress = address;
    }
}
