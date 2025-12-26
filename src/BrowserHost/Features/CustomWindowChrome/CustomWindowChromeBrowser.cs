using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowser : Browser<CustomWindowChromeBackendApi>
{
    public override CustomWindowChromeBackendApi Api { get; }

    public CustomWindowChromeBrowser()
        : base("/", disableContextMenu: true)
    {
        Api = new CustomWindowChromeBackendApi();
    }
}
