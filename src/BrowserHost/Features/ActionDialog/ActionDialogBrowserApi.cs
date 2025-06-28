using BrowserHost.CefInfrastructure;
using System.Threading.Channels;

namespace BrowserHost.Features.ActionDialog;

public record ActionDialogDismissedEvent();
public record NavigationStartedEvent(string Address, bool UseCurrentTab);

public class ActionDialogBrowserApi(ActionDialogBrowser browser) : BrowserApi(browser)
{
    public Channel<ActionDialogDismissedEvent> ActionDialogDismissedChannel { get; } = Channel.CreateUnbounded<ActionDialogDismissedEvent>();
    public Channel<NavigationStartedEvent> NavigationStartedChannel { get; } = Channel.CreateUnbounded<NavigationStartedEvent>();

    public void Navigate(string url, bool useCurrentTab) =>
        NavigationStartedChannel.Writer.TryWrite(new NavigationStartedEvent(url, useCurrentTab));

    public void DismissActionDialog() =>
        ActionDialogDismissedChannel.Writer.TryWrite(new ActionDialogDismissedEvent());
}
