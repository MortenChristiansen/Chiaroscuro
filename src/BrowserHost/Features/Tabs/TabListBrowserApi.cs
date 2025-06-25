using BrowserHost.CefInfrastructure;
using System.Threading.Channels;

namespace BrowserHost.Features.Tabs;

public record TabActivatedEvent(string TabId);
public record TabClosedEvent(string TabId);
public record TabPositionChangedEvent(string TabId, int NewIndex);

public class TabListBrowserApi(TabListBrowser browser) : BrowserApi(browser)
{
    public Channel<TabActivatedEvent> TabActivatedChannel { get; } = Channel.CreateUnbounded<TabActivatedEvent>();
    public Channel<TabClosedEvent> TabClosedChannel { get; } = Channel.CreateUnbounded<TabClosedEvent>();
    public Channel<TabPositionChangedEvent> TabPositionChanged { get; } = Channel.CreateUnbounded<TabPositionChangedEvent>();

    public void ActivateTab(string tabId) =>
        TabActivatedChannel.Writer.TryWrite(new TabActivatedEvent(tabId));

    public void CloseTab(string tabId) =>
        TabClosedChannel.Writer.TryWrite(new TabClosedEvent(tabId));

    public void ReorderTab(string tabId, int newIndex) =>
        TabPositionChanged.Writer.TryWrite(new TabPositionChangedEvent(tabId, newIndex));
}
