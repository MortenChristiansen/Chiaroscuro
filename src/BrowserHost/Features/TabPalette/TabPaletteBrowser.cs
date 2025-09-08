using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Features.TabPalette.DomainCustomization;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser : Browser
{
    public FindTextBrowserApi FindTextApi { get; } = new();
    public TabCustomizationBrowserApi TabCustomizationApi { get; } = new();
    public DomainCustomizationBrowserApi DomainCustomizationApi { get; } = new();

    public TabPaletteBrowser()
        : base("/tab-palette", disableContextMenu: true)
    {
        RegisterSecondaryApi(FindTextApi, "findTextApi");
        RegisterSecondaryApi(TabCustomizationApi, "tabCustomizationApi");
        RegisterSecondaryApi(DomainCustomizationApi, "domainCustomizationApi");
    }

    public void Init()
    {
        CallClientApi("init");
    }
}
