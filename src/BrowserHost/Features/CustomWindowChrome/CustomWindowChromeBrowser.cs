using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowser : Browser<CustomWindowChromeBrowserApi>
{
    public override CustomWindowChromeBrowserApi Api { get; }

    public CustomWindowChromeBrowser()
        : base("/")
    {
        Api = new CustomWindowChromeBrowserApi();
    }

    public void ChangeAddress(string? address) =>
        CallClientApi("changeAddress", address.ToJsonString());
}
