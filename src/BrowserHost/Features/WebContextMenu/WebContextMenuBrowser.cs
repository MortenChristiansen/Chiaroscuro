using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.WebContextMenu;

public record ContextMenuParameters(string? LinkUrl, string? ImageSourceUrl);

public class WebContextMenuBrowser : Browser<WebContextMenuBackendApi>
{
    public override WebContextMenuBackendApi Api { get; } = new(App.PubSub);

    public WebContextMenuBrowser()
        : base("/context-menu", disableContextMenu: true)
    {
    }
}
