using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : Browser
{
    private readonly ActionContextBrowser _actionContextBrowser;

    public string Id { get; } = $"{Guid.NewGuid()}";
    public string? Favicon { get; private set; }
    public string? ManualAddress { get; private set; }

    public TabBrowser(string address, ActionContextBrowser actionContextBrowser, bool setManualAddress)
    {
        Address = address;
        if (setManualAddress)
            ManualAddress = address;

        // Set initial title for file URLs
        if (IsFileUrl(address))
        {
            var fileName = GetFileNameFromUrl(address);
            if (!string.IsNullOrEmpty(fileName))
                Title = fileName;
        }

        TitleChanged += OnTitleChanged;
        LoadingStateChanged += OnLoadingStateChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _actionContextBrowser = actionContextBrowser;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);
        RequestHandler = new RequestHandler(Id);

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _actionContextBrowser.UpdateTabTitle(Id, (string)e.NewValue);
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        PubSub.Publish(new TabFaviconUrlChangedEvent(Id, Favicon));
        Dispatcher.BeginInvoke(() => _actionContextBrowser.UpdateTabFavicon(Id, Favicon));
    }

    private void OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
    {
        PubSub.Publish(new TabLoadingStateChangedEvent(Id, e.IsLoading));
    }

    public void SetAddress(string address, bool setManualAddress)
    {
        Address = address;
        if (setManualAddress)
            ManualAddress = address;
    }

    private static bool IsFileUrl(string url)
    {
        return url.StartsWith("file://", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetFileNameFromUrl(string url)
    {
        if (!IsFileUrl(url))
            return null;

        try
        {
            var uri = new Uri(url);
            var localPath = uri.LocalPath;
            return Path.GetFileName(localPath);
        }
        catch
        {
            return null;
        }
    }
}
