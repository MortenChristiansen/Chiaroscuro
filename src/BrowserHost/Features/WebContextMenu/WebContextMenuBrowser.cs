using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.WebContextMenu;

public record ContextMenuParameters(string? LinkUrl, string? ImageSourceUrl);

public class WebContextMenuBrowser : Browser<WebContextMenuBackendApi>
{
    public override WebContextMenuBackendApi Api { get; }

    public WebContextMenuBrowser(PubSub pubSub)
        : base("/context-menu", disableContextMenu: true)
    {
        Api = new WebContextMenuBackendApi(pubSub);
    }
}
