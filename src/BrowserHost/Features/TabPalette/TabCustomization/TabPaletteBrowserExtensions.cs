using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public static class TabPaletteBrowserExtensions
{
    public static void InitCustomTitle(this TabPaletteBrowser browser, string? title)
    {
        browser.CallClientApi("initCustomTitle", title.ToJsonString());
    }
}
