using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.CustomWindowChrome;

public class CustomWindowChromeBrowserApi(BaseBrowser customChromeBrowser) : BrowserApi(customChromeBrowser)
{
    public void ChangeAddress(string? address) =>
        CallClientApi("changeAddress", address.ToJsonString());

    public void UpdateLoadingState(bool isLoading) =>
        CallClientApi("updateLoadingState", isLoading.ToJsonBoolean());

    public void UpdateWindowState(bool isMaximized) =>
        CallClientApi("updateWindowState", isMaximized.ToJsonBoolean());
}
