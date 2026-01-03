using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System.Diagnostics;
using System.Windows.Input;

namespace BrowserHost.Features.DevTool;

public class DevToolFeature(MainWindow window, PubSub pubSub, IBrowserContext browserContext) : Feature(window, pubSub)
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
        ToggleDevTools(browserContext.CurrentTab);
    }

    private void ToggleActionContextDevTools()
    {
        browserContext.ToggleActionContextDevTools();
    }

    private void ToggleTabPalettetDevTools()
    {
        browserContext.ToggleTabPaletteDevTools();
    }

    private static void ToggleDevTools(ITabBrowser? browser)
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