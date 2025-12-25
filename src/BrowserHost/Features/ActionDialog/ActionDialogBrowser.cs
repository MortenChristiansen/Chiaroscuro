using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogBrowser : Browser<ActionDialogBackendApi>
{
    public override ActionDialogBackendApi Api { get; }

    public ActionDialogBrowser()
        : base("/action-dialog", disableContextMenu: true)
    {
        Api = new ActionDialogBackendApi();
    }
}