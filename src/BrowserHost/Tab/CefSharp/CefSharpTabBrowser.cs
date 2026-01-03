using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.Permissions;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.WebContextMenu;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace BrowserHost.Tab.CefSharp;

public class CefSharpTabBrowser : Browser
{
    private readonly TabsBrowserApi _tabsBrowserApi;
    private readonly PubSub _pubSub;
    private readonly bool _isChildBrowser;
    private readonly DragDropFeature _dragDropFeature;

    public string Id { get; }
    public string? Favicon { get; private set; }
    public string? ManualAddress { get; private set; }

    public CefSharpTabBrowser(string id, string address, TabsBrowserApi tabsBrowserApi, PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        Id = id;
        Favicon = favicon;
        _isChildBrowser = isChildBrowser;
        _pubSub = pubSub;
        _dragDropFeature = MainWindow.Instance.GetFeature<DragDropFeature>();
        SetAddress(address, setManualAddress);

        TitleChanged += OnTitleChanged;
        LoadingStateChanged += OnLoadingStateChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _tabsBrowserApi = tabsBrowserApi;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);
        RequestHandler = new RequestHandler(Id, isChildBrowser, pubSub);
        LifeSpanHandler = new PopupLifeSpanHandler(this, pubSub);
        FindHandler = new FindHandler(pubSub);
        PermissionHandler = new CefSharpPermissionHandler();
        MenuHandler = new WebContentContextMenuHandler();

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var newTitle = e.NewValue as string;

        // It seems there is a bug in the PDF viewer that wants to change the title of the browser to something wrong, so we always set it to the file name
        if (sender is CefSharpTabBrowser tb && tb.Address.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            newTitle = GetFileDisplayName(tb.Address);

        if (!_isChildBrowser && !IsNavigationBlocked)
            _tabsBrowserApi.UpdateTabTitle(Id, newTitle);
    }

    private static string GetFileDisplayName(string fileUri)
    {
        if (string.IsNullOrWhiteSpace(fileUri))
            return fileUri;

        // Try to parse as a file:// URI. If parsing fails, fall back to the raw value.
        if (!Uri.TryCreate(fileUri, UriKind.Absolute, out var uri) || !uri.IsFile)
            return fileUri;

        // LocalPath is already unescaped for typical file URIs.
        var localPath = uri.LocalPath;
        if (string.IsNullOrWhiteSpace(localPath))
            return fileUri;

        // Prefer just the filename for a concise tab title.
        var name = Path.GetFileName(localPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? localPath : name;
    }


    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        if (!_isChildBrowser && !IsNavigationBlocked)
        {
            _pubSub.Publish(new TabFaviconUrlChangedEvent(Id, Favicon));
            Dispatcher.BeginInvoke(() => _tabsBrowserApi.UpdateTabFavicon(Id, Favicon));
        }
    }

    private void OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
    {
        _pubSub.Publish(new TabLoadingStateChangedEvent(Id, e.IsLoading));
    }

    public void SetAddress(string address, bool setManualAddress)
    {
        Address = address;
        if (setManualAddress)
            ManualAddress = address;
    }

    private DateTimeOffset? _navigationBlockedUntil;
    private bool IsNavigationBlocked => _navigationBlockedUntil.HasValue && DateTimeOffset.UtcNow < _navigationBlockedUntil.Value;
    public void ApplyTemporaryNavigationBlock()
    {
        // This is a workaround to behavior seen on Google product links that uses JS to change the url
        // after opening in a new tab. This causes us to both open a new browser tab and change the url
        // of the existing tab. To prevent this, we block any navigation for a short time in the original
        // tab.

        _navigationBlockedUntil = DateTimeOffset.UtcNow.AddSeconds(3);
    }

    protected override void OnAddressChanged(string? oldValue, string newValue)
    {
        //if (oldValue?.StartsWith("file://") == true && newValue.StartsWith("file://") == true)
        //    return;

        if (IsNavigationBlocked)
        {
            GetBrowser().GoBack();
            return;
        }

        base.OnAddressChanged(oldValue, newValue);
    }

    public void RegisterContentPageApi<TApi>(TApi api, string name) where TApi : BackendApi
    {
        RegisterSecondaryApi(api, name);
    }
}
