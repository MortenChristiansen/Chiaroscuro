using BrowserHost.CefInfrastructure;
using System.Threading.Channels;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogBrowser : BaseBrowser<ActionDialogBrowserApi>
{
    public override ActionDialogBrowserApi Api { get; }

    public ActionDialogBrowser()
        : base("/action-dialog")
    {
        Api = new ActionDialogBrowserApi(this);
    }
}

public record ActionDialogDismissedEvent();
public record NavigationStartedEvent(string Address);

public class ActionDialogBrowserApi(ActionDialogBrowser browser) : BrowserApi(browser)
{
    public Channel<ActionDialogDismissedEvent> ActionDialogDismissedChannel { get; } = Channel.CreateUnbounded<ActionDialogDismissedEvent>();
    public Channel<NavigationStartedEvent> NavigationStartedChannel { get; } = Channel.CreateUnbounded<NavigationStartedEvent>();

    public void Navigate(string url) =>
        NavigationStartedChannel.Writer.TryWrite(new NavigationStartedEvent(url));

    public void DismissActionDialog() =>
        ActionDialogDismissedChannel.Writer.TryWrite(new ActionDialogDismissedEvent());
}