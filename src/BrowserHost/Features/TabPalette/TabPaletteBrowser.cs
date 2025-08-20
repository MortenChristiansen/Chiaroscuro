using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser : Browser
{
    public FindTextBrowserApi FindTextApi { get; } = new();
    public TabCustomizationBrowserApi TabCustomizationApi { get; } = new();

    public TabPaletteBrowser()
        : base("/tab-palette", disableContextMenu: true)
    {
        RegisterSecondaryApi(FindTextApi, "findTextApi");
        RegisterSecondaryApi(TabCustomizationApi, "tabCustomizationApi");
    }

    public void Init()
    {
        CallClientApi("init");
    }
}
