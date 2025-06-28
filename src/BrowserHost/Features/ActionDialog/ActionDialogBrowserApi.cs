using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

public record ActionDialogDismissedEvent();
public record NavigationStartedEvent(string Address, bool UseCurrentTab);

public class ActionDialogBrowserApi(ActionDialogBrowser browser) : BrowserApi(browser)
{
    public void Navigate(string url, bool useCurrentTab) =>
        PubSub.Publish(new NavigationStartedEvent(url, useCurrentTab));

    public void DismissActionDialog() =>
        PubSub.Publish(new ActionDialogDismissedEvent());
}
