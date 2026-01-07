using BrowserHost.Tab;

namespace BrowserHost.Features.ActionContext.Tabs;

public interface ITabsHost
{
    void PreloadTab(ITabBrowser tab);
    void RemovePreloadedTab(string tabId);
}
