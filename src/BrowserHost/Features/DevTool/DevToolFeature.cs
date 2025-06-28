using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System.Windows.Input;

namespace BrowserHost.Features.DevTool;

public class DevToolFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.Tabs.Api)
{
    public override void Register()
    {
        PubSub.Subscribe<TabClosedEvent>(e => HandleTabClosed(e.Tab));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F12)
        {
            ToggleDevTools();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void ToggleDevTools()
    {
        var currentTab = Window.CurrentTab;
        if (currentTab == null) return;

        var browserHost = currentTab.GetBrowserHost();
        if (browserHost != null)
        {
            if (browserHost.HasDevTools)
                browserHost.CloseDevTools();
            else
                browserHost.ShowDevTools();
        }
    }

    private void HandleTabClosed(TabBrowser tab)
    {
        var browserHost = tab?.GetBrowserHost();
        if (browserHost?.HasDevTools == true)
            browserHost.CloseDevTools();
    }
}