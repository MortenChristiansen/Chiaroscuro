using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationBrowserApi(BaseBrowser tabPaletteBrowser) : BrowserApi(tabPaletteBrowser)
{
    public void InitCustomSettings(TabCustomizationDataV1 settings) =>
        CallClientApi("initCustomSettings", settings.ToJsonObject());
}
