using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System.Collections.Generic;

namespace BrowserHost.Features.ActionDialog;

public class ActionDialogBrowserApi(BaseBrowser actionDialogBrowser) : BrowserApi(actionDialogBrowser)
{
    public void ShowActionDialog() =>
        CallClientApi("showDialog");

    public void UpdateSuggestions(List<NavigationSuggestion> suggestions) =>
        CallClientApi("updateSuggestions", suggestions.ToJsonObject());
}
