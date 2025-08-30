using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using CefSharp;
using System.Diagnostics;
using System.Windows.Input;

namespace BrowserHost.Features.DevTool;

public class DevToolFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Subscribe<TabClosedEvent>(e => e.Tab.CloseDevTools());
        PubSub.Subscribe<TabActivatedEvent>(e => e.PreviousTab?.CloseDevTools());
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F12)
        {
            ToggleDevTools();
            return true;
        }

        if (e.Key == Key.F11 && Debugger.IsAttached)
        {
            ToggleActionContextDevTools();
            return true;
        }

        // For some reason, F10 needs to be handled as SystemKey
        if ((e.Key == Key.F10 || e.SystemKey == Key.F10) && Debugger.IsAttached)
        {
            ToggleTabPalettetDevTools();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void ToggleDevTools()
    {
        var currentTab = Window.CurrentTab;
        if (currentTab == null) return;

        ToggleDevTools(currentTab);
    }

    private void ToggleActionContextDevTools()
    {
        ToggleDevTools(Window.ActionContext.GetBrowserHost());
    }

    private void ToggleTabPalettetDevTools()
    {
        ToggleDevTools(Window.TabPaletteBrowserControl.GetBrowserHost());
    }

    private static void ToggleDevTools(IBrowserHost browserHost)
    {
        if (browserHost != null)
        {
            if (browserHost.HasDevTools)
                browserHost.CloseDevTools();
            else
                browserHost.ShowDevTools();
        }
    }

    private static void ToggleDevTools(TabBrowser? browser)
    {
        if (browser != null)
        {
            if (browser.HasDevTools)
                browser.CloseDevTools();
            else
                browser.ShowDevTools();
        }
    }
}