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
        LifeSpanHandler = new PopupLifeSpanHandler(Id);
        FindHandler = new FindHandler();
        PermissionHandler = new CefSharpPermissionHandler();
        MenuHandler = new WebContentContextMenuHandler();

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);
    }

    private void OnTitleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!_isChildBrowser)
            _actionContextBrowser.UpdateTabTitle(Id, (string)e.NewValue);
    }

    private void OnFaviconAddressesChanged(IList<string> addresses)
    {
        Favicon = addresses.FirstOrDefault();
        if (!_isChildBrowser)
        {
            PubSub.Publish(new TabFaviconUrlChangedEvent(Id, Favicon));
            Dispatcher.BeginInvoke(() => _actionContextBrowser.UpdateTabFavicon(Id, Favicon));
        }
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

    private static string _lastChildUrl = string.Empty;

    protected override void OnAddressChanged(string oldValue, string newValue)
    {
        if (_isChildBrowser)
            _lastChildUrl = newValue;

        // This is a workaround to behavior seen on Google product links that uses JS to change the url
        // after opening in a new tab. This causes us to both open a child browser tab and change the url
        // of the parent tab. To prevent this, we check if the new url matches the last child url opened.
        // In some cases the child url changes to a google redirect link, while the parent tab is changed
        // to the final destination url. To handle this, we also check for the google redirect pattern.
        if (!_isChildBrowser && (newValue == _lastChildUrl || _lastChildUrl.StartsWith("https://www.google.com/aclk?")))
        {
            _lastChildUrl = "";
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
