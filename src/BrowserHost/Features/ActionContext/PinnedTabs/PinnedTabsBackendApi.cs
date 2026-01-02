using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public record PinTabCommand(string TabId) : ICommand;
public record UnpinTabCommand(string TabId) : ICommand;

public record TabPinnedEvent(string TabId) : IEvent;
public record TabUnpinnedEvent(string TabId) : IEvent;

public class PinnedTabsBackendApi(PubSub pubSub) : BackendApi
{
    public void UnpinTab(string tabId) =>
        pubSub.Send(new UnpinTabCommand(tabId));

    public void ActivateTab(string tabId) =>
        pubSub.Send(new ActivateTabCommand(tabId));
}
