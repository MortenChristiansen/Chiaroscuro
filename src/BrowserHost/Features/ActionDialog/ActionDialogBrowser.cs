using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogBrowser : Browser<ActionDialogBackendApi>
{
    public override ActionDialogBackendApi Api { get; }

    public ActionDialogBrowser(PubSub pubSub)
        : base("/action-dialog", disableContextMenu: true)
    {
        Api = new ActionDialogBackendApi(pubSub);
    }
}