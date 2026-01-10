using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowser : Browser<CustomWindowChromeBackendApi>
{
    public override CustomWindowChromeBackendApi Api { get; }

    public CustomWindowChromeBrowser(PubSub pubSub)
        : base("/", disableContextMenu: true)
    {
        Api = new CustomWindowChromeBackendApi(pubSub);
    }
}
