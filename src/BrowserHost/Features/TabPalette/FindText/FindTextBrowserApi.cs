using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.TabPalette.FindText;

public class FindTextBrowserApi(BaseBrowser tabPaletteBrowser) : BrowserApi(tabPaletteBrowser)
{
    public void FindStatusChanged(int? totalMatches) =>
        CallClientApi("findStatusChanged", $"{totalMatches}");

    public void FocusFindTextInput() =>
        CallClientApi("focusFindTextInput");
}
