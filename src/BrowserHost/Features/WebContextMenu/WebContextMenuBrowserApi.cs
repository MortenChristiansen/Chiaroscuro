using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuBrowserApi(BaseBrowser webContextMenuBrowser) : BrowserApi(webContextMenuBrowser)
{
    public void SetParameters(ContextMenuParameters parameters) =>
        CallClientApi("setParameters", parameters.ToJsonObject());
}
