using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowserApi(BaseBrowser tabPaletteBrowser) : BrowserApi(tabPaletteBrowser)
{
    public void Init() =>
        CallClientApi("init");
}
