using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogBrowser : Browser<ActionDialogBrowserApi>
{
    public override ActionDialogBrowserApi Api { get; }

    public ActionDialogBrowser()
        : base("/action-dialog")
    {
        Api = new ActionDialogBrowserApi(this);
    }
}