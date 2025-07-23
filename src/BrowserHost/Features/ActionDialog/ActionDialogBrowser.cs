using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System.Collections.Generic;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogBrowser : Browser<ActionDialogBrowserApi>
{
    public override ActionDialogBrowserApi Api { get; }

    public ActionDialogBrowser()
        : base("/action-dialog", disableContextMenu: true)
    {
        Api = new ActionDialogBrowserApi();
    }

    public void UpdateSuggestions(List<NavigationSuggestion> suggestions)
    {
        CallClientApi("updateSuggestions", suggestions.ToJsonObject());
    }
}