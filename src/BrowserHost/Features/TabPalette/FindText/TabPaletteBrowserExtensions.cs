namespace BrowserHost.Features.TabPalette.FindText;

public static class TabPaletteBrowserExtensions
{
    public static void FindStatusChanged(this TabPaletteBrowser browser, int? totalMatches)
    {
        browser.CallClientApi("findStatusChanged", $"{totalMatches}");
    }

    public static void FocusFindTextInput(this TabPaletteBrowser browser)
    {
        browser.CallClientApi("focusFindTextInput");
    }
}
