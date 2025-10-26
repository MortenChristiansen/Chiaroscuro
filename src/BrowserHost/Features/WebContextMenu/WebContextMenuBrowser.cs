using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuBrowser : Browser<WebContextMenuBrowserApi>
{
    public override WebContextMenuBrowserApi Api { get; } = new();

    public WebContextMenuBrowser()
        : base("/context-menu", disableContextMenu: true)
    {
    }

    public void SetParameters(ContextMenuParameters parameters)
    {
        CallClientApi("setParameters", parameters.ToJsonObject());
    }
}
