using BrowserHost.Features.Tabs;
using CefSharp;
using System.Windows.Input;

namespace BrowserHost.Features.DevTool;

public class DevToolFeature(MainWindow window) : Feature<TabListBrowserApi>(window, window.Tabs.Api)
{
    private TabBrowser? _devToolsTab = null;

    public override void Register()
    {
        // Listen for tab activation events to move dev tools to the new current tab
        _ = Listen(Api.TabActivatedChannel,
            e => HandleTabActivated(e.TabId),
            dispatchToUi: true
        );

        // Listen for tab closed events to close dev tools if needed
        _ = Listen(Api.TabClosedChannel,
            e => HandleTabClosed(e.TabId),
            dispatchToUi: true
        );
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

        // Always try to close dev tools first to handle manual closure scenarios
        // This ensures we're synchronized with the actual dev tools state
        var browserHost = currentTab.GetBrowserHost();
        if (browserHost != null)
        {
            browserHost.CloseDevTools();
        }

        // If we had tracked dev tools as open, they're now closed
        if (_devToolsTab != null)
        {
            _devToolsTab = null;
        }
        else
        {
            // Dev tools weren't tracked as open, so they should now be opened
            if (browserHost != null)
            {
                browserHost.ShowDevTools();
                _devToolsTab = currentTab;
            }
        }
    }

    private void OpenDevTools()
    {
        var currentTab = Window.CurrentTab;
        if (currentTab == null) return;

        // Close any existing dev tools first
        if (_devToolsTab != null)
        {
            CloseDevTools();
        }

        // Open dev tools for the current tab
        var browserHost = currentTab.GetBrowserHost();
        if (browserHost != null)
        {
            browserHost.ShowDevTools();
            _devToolsTab = currentTab;
        }
    }

    private void CloseDevTools()
    {
        if (_devToolsTab != null)
        {
            var browserHost = _devToolsTab.GetBrowserHost();
            browserHost?.CloseDevTools();
        }

        _devToolsTab = null;
    }

    private void HandleTabActivated(string tabId)
    {
        var newCurrentTab = Window.CurrentTab;

        // If dev tools are open and the current tab changed
        if (_devToolsTab != null && newCurrentTab != null && newCurrentTab != _devToolsTab)
        {
            // Close dev tools for the old tab
            var oldBrowserHost = _devToolsTab.GetBrowserHost();
            oldBrowserHost?.CloseDevTools();

            // Open dev tools for the new current tab
            var newBrowserHost = newCurrentTab.GetBrowserHost();
            if (newBrowserHost != null)
            {
                newBrowserHost.ShowDevTools();
                _devToolsTab = newCurrentTab;
            }
        }
    }

    private void HandleTabClosed(string tabId)
    {
        // If the closed tab had dev tools open, close them
        if (_devToolsTab != null && _devToolsTab.Id == tabId)
        {
            CloseDevTools();
        }
    }
}