using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DragDrop;
using BrowserHost.Features.FileDownloads;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace BrowserHost.Features.Tabs;

public class TabBrowser : Browser
{
    private readonly ActionContextBrowser _actionContextBrowser;
    private readonly NavigationBrowserApi _navigationApi;

    public string Id { get; } = $"{Guid.NewGuid()}";
    public string? Favicon { get; private set; }
    public string? ManualAddress { get; private set; }

    public TabBrowser(string address, ActionContextBrowser actionContextBrowser, bool setManualAddress)
    {
        _navigationApi = new NavigationBrowserApi();
        SetAddress(address, setManualAddress);

        TitleChanged += OnTitleChanged;
        LoadingStateChanged += OnLoadingStateChanged;

        DisplayHandler = new FaviconDisplayHandler(OnFaviconAddressesChanged);
        _actionContextBrowser = actionContextBrowser;

        var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        DownloadHandler = new DownloadHandler(downloadsPath);
        RequestHandler = new RequestHandler(Id);

        BrowserSettings.BackgroundColor = Cef.ColorSetARGB(255, 255, 255, 255);
        
        // Register the navigation API for middle mouse click handling
        RegisterSecondaryApi(_navigationApi, "navigationApi");
        
        // Inject JavaScript to handle middle mouse clicks after the page loads
        FrameLoadEnd += OnFrameLoadEnd;
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

    protected override void OnAddressChanged(string oldValue, string newValue)
    {
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

    private void OnFrameLoadEnd(object? sender, FrameLoadEndEventArgs e)
    {
        // Only inject into the main frame to avoid duplicate handlers
        if (e.Frame.IsMain)
        {
            var middleClickScript = @"
                (function() {
                    // Add event listener for middle mouse clicks on links
                    document.addEventListener('mousedown', function(event) {
                        // Check if middle mouse button (button 1) was clicked
                        if (event.button === 1) {
                            // Find the closest anchor tag
                            var target = event.target;
                            while (target && target.tagName !== 'A') {
                                target = target.parentElement;
                                // Prevent infinite loop if we reach document
                                if (target === document) {
                                    target = null;
                                    break;
                                }
                            }
                            
                            // If we found an anchor tag with an href
                            if (target && target.href && target.href !== '') {
                                // Prevent default behavior (following the link)
                                event.preventDefault();
                                event.stopPropagation();
                                
                                // Call the C# method to open in new tab
                                try {
                                    if (window.navigationApi && window.navigationApi.OpenLinkInNewTab) {
                                        window.navigationApi.OpenLinkInNewTab(target.href);
                                    } else {
                                        console.warn('navigationApi not available for middle click');
                                    }
                                } catch (error) {
                                    console.error('Error opening link in new tab:', error);
                                }
                            }
                        }
                    }, true);
                    
                    console.log('Middle mouse click handler installed');
                })();
            ";
            
            this.ExecuteScriptAsync(middleClickScript);
        }
    }
}
