using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowser : Browser<CustomWindowChromeBackendApi>
{
    public override CustomWindowChromeBackendApi Api { get; } = new(App.PubSub);

    public CustomWindowChromeBrowser()
        : base("/", disableContextMenu: true)
    {
    }
}
