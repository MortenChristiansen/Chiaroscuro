namespace BrowserHost.Features.TabPalette.FindText;

public static class TabPaletteBrowserExtensions
{
    public static void FindStatusChanged(this TabPaletteBrowser browser, int? totalMatches)
    {
        browser.CallClientApi("findStatusChanged", $"{totalMatches}");
    }
}
