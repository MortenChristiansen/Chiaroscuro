using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Tab;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabsHost : ITabsHost
{
    public List<string> PreloadedTabIds { get; } = [];
    public List<string> RemovedTabIds { get; } = [];

    public void PreloadTab(ITabBrowser tab)
    {
        if (!PreloadedTabIds.Contains(tab.Id))
            PreloadedTabIds.Add(tab.Id);
    }

    public void RemovePreloadedTab(string tabId)
    {
        RemovedTabIds.Add(tabId);
        PreloadedTabIds.RemoveAll(id => id == tabId);
    }
}
