using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System.Windows.Input;

namespace BrowserHost.Features.DevTool;

public class DevToolFeature(MainWindow window) : Feature(window)
{
    public override void Register()
    {
        PubSub.Subscribe<TabClosedEvent>(e => CloseDevTools(e.Tab));
        PubSub.Subscribe<TabActivatedEvent>(e => CloseDevTools(e.CurrentTab));
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

    private static void CloseDevTools(TabBrowser? tab)
    {
        if (tab == null) return;

        tab.GetBrowserHost()?.CloseDevTools();
    }
}