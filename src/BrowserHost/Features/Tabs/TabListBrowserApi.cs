using BrowserHost.CefInfrastructure;
using System.Threading.Channels;

namespace BrowserHost.Features.Tabs;

public record TabActivatedEvent(string TabId);

public class TabListBrowserApi(TabListBrowser browser) : BrowserApi(browser)
{
    public Channel<TabActivatedEvent> TabActivatedChannel { get; } = Channel.CreateUnbounded<TabActivatedEvent>();

    public void ActivateTab(string tabId) =>
        TabActivatedChannel.Writer.TryWrite(new TabActivatedEvent(tabId));
}
