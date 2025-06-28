using BrowserHost.Features.Tabs;
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
        // Instead of relying on internal state, check if we have a dev tools tab tracked
        // This is more robust against manual closure of dev tools window
        if (_devToolsTab != null)
        {
            CloseDevTools();
        }
        else
        {
            OpenDevTools();
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