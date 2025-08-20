using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.FindText;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser : Browser
{
    public FindTextBrowserApi FindTextApi { get; } = new();

    public TabPaletteBrowser()
        : base("/tab-palette", disableContextMenu: true)
    {
        RegisterSecondaryApi(FindTextApi, "findTextApi");
    }

    public void Init()
    {
        CallClientApi("init");
    }
}
