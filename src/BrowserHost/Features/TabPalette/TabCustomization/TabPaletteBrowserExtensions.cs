using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public static class TabPaletteBrowserExtensions
{
    public static void InitCustomSettings(this TabPaletteBrowser browser, TabCustomizationDataV1 settings)
    {
        browser.CallClientApi("initCustomSettings", settings.ToJsonObject());
    }
}
