using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
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
using System.Linq;
using System.Windows;

namespace BrowserHost.Tab.CefSharp;

public class CefSharpTabBrowser : Browser
{
    private readonly ActionContextBrowser _actionContextBrowser;
    private readonly bool _isChildBrowser;

    public string Id { get; }
    public string? Favicon { get; private set; }
    public string? ManualAddress { get; private set; }

    public CefSharpTabBrowser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        Id = id;
        Favicon = favicon;
        _isChildBrowser = isChildBrowser;
        SetAddress(address, setManualAddress);

        TitleChanged += OnTitleChanged;
        LoadingStateChanged += OnLoadingStateChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _actionContextBrowser = actionContextBrowser;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);
        RequestHandler = new RequestHandler(Id, isChildBrowser);
        LifeSpanHandler = new PopupLifeSpanHandler(this);
        FindHandler = new FindHandler();
        PermissionHandler = new CefSharpPermissionHandler();
        MenuHandler = new WebContentContextMenuHandler();

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!_isChildBrowser && !IsNavigationBlocked)
            _actionContextBrowser.UpdateTabTitle(Id, (string)e.NewValue);
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        if (!_isChildBrowser && !IsNavigationBlocked)
        {
            PubSub.Instance.Publish(new TabFaviconUrlChangedEvent(Id, Favicon));
            Dispatcher.BeginInvoke(() => _actionContextBrowser.UpdateTabFavicon(Id, Favicon));
        }
    }

    private void OnLoadingStateChanged(object? sender, LoadingStateChangedEventArgs e)
    {
        PubSub.Instance.Publish(new TabLoadingStateChangedEvent(Id, e.IsLoading));
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

    protected override void OnAddressChanged(string oldValue, string newValue)
    {
        if (IsNavigationBlocked)
        {
            GetBrowser().GoBack();
            return;
        }

        if (DragDropFeature.IsDragging && oldValue != null && newValue.StartsWith("file://"))
        {
            // This is a workaround to prevent the current address from being set
            // when dragging and dropping files into the browser. Instead, we want
            // open a new tab with the file URL. This is not directly possible,
            // so we have to revert the change 
            GetBrowser().GoBack();
        }
        else
        {
            base.OnAddressChanged(oldValue, newValue);
        }
    }

    public void RegisterContentPageApi<TApi>(TApi api, string name) where TApi : BrowserApi
    {
        RegisterSecondaryApi(api, name);
    }
}
