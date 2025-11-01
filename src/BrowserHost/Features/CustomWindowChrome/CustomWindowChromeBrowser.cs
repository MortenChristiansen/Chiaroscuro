using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowser : Browser<CustomWindowChromeBrowserApi>
{
    public override CustomWindowChromeBrowserApi Api { get; }

    public CustomWindowChromeBrowser()
        : base("/", disableContextMenu: true)
    {
        Api = new CustomWindowChromeBrowserApi();
    }

    public void ChangeAddress(string? address) =>
        CallClientApi("changeAddress", address.ToJsonString());

    public void UpdateLoadingState(bool isLoading) =>
        CallClientApi("updateLoadingState", isLoading.ToJsonBoolean());

    public void UpdateWindowState(bool isMaximized) =>
        CallClientApi("updateWindowState", isMaximized.ToJsonBoolean());
}
