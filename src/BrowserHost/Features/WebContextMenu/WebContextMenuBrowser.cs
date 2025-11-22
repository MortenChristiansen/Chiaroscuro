using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.WebContextMenu;

public record ContextMenuParameters(string? LinkUrl, string? ImageSourceUrl);

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
