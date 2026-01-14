using BrowserHost.Tab;
using System;
using System.Linq;

namespace BrowserHost.Features.ActionContext.Tabs;

public class MainWindowTabsHost(MainWindow window) : ITabsHost
{
    public void PreloadTab(ITabBrowser tab)
    {
        if (tab is not TabBrowser tabBrowser)
            throw new InvalidOperationException("Only TabBrowser instances can be preloaded.");

        var host = window.PreloadTabsHost;
        if (host.Children.OfType<TabBrowser>().Any(tb => tb.Id == tabBrowser.Id))
            return;

        host.Children.Add(tabBrowser);
    }

    public void RemovePreloadedTab(string tabId)
    {
        var host = window.PreloadTabsHost;
        foreach (var child in host.Children.OfType<TabBrowser>().Where(tb => tb.Id == tabId).ToArray())
            host.Children.Remove(child);
    }
}
