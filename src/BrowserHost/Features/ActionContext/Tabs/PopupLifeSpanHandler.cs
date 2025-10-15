using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using CefSharp;
using System.Windows;

namespace BrowserHost.Features.ActionContext.Tabs;

public sealed class PopupLifeSpanHandler(string tabId) : ILifeSpanHandler
{
    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        return false; // default
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
    }

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
    }

    public bool OnBeforePopup(
        IWebBrowser chromiumWebBrowser,
        IBrowser browser,
        IFrame frame,
        string targetUrl,
        string targetFrameName,
        WindowOpenDisposition targetDisposition,
        bool userGesture,
        IPopupFeatures popupFeatures,
        IWindowInfo windowInfo,
        IBrowserSettings browserSettings,
        ref bool noJavascriptAccess,
        out IWebBrowser? newBrowser)
    {
        // Do not create a native popup. We handle navigation ourselves.
        newBrowser = null;

        switch (targetDisposition)
        {
            case WindowOpenDisposition.NewBackgroundTab:
                PubSub.Publish(new NavigationStartedEvent(targetUrl, UseCurrentTab: false, SaveInHistory: true));
                return true;
            case WindowOpenDisposition.NewForegroundTab:
            case WindowOpenDisposition.NewWindow:
            case WindowOpenDisposition.NewPopup:
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    var owner = MainWindow.Instance;
                    var win = new ChildBrowserWindow(targetUrl, tabId)
                    {
                        Owner = owner
                    };
                    win.Show();
                });
                return true;
            default:
                return false; // let default behavior occur (rare)
        }
    }
}
