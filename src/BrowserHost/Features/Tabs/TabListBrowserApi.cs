using BrowserHost.CefInfrastructure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;

namespace BrowserHost.Features.Tabs;

public record TabActivatedEvent(string TabId);
public record TabClosedEvent(string TabId);
public record TabPositionChangedEvent(string TabId, int NewIndex);
public record TabsChangedEvent(TabStateDto[] Tabs);

public class TabListBrowserApi(TabListBrowser browser) : BrowserApi(browser)
{
    public Channel<TabActivatedEvent> TabActivatedChannel { get; } = Channel.CreateUnbounded<TabActivatedEvent>();
    public Channel<TabClosedEvent> TabClosedChannel { get; } = Channel.CreateUnbounded<TabClosedEvent>();
    public Channel<TabPositionChangedEvent> TabPositionChangedChannel { get; } = Channel.CreateUnbounded<TabPositionChangedEvent>();
    public Channel<TabsChangedEvent> TabsChangedChannel { get; } = Channel.CreateUnbounded<TabsChangedEvent>();

    public void ActivateTab(string tabId) =>
        TabActivatedChannel.Writer.TryWrite(new TabActivatedEvent(tabId));

    public void CloseTab(string tabId) =>
        TabClosedChannel.Writer.TryWrite(new TabClosedEvent(tabId));

    public void ReorderTab(string tabId, int newIndex) =>
        TabPositionChangedChannel.Writer.TryWrite(new TabPositionChangedEvent(tabId, newIndex));

    public void TabsChanged(List<object> tabs)
    {
        TabsChangedChannel.Writer.TryWrite(new TabsChangedEvent(
            [.. tabs.Select((dynamic tab) => new TabStateDto(tab.Address, tab.Title, tab.Favicon, tab.IsActive))]
        ));
    }
}
