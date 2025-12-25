using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser : Browser
{
    public FindTextBackendApi FindTextApi { get; } = new();
    public TabCustomizationBackendApi TabCustomizationApi { get; } = new();
    public DomainCustomizationBackendApi DomainCustomizationApi { get; } = new();

    public TabPaletteBrowser()
        : base("/tab-palette", disableContextMenu: true)
    {
        RegisterSecondaryApi(FindTextApi, "findTextApi");
        RegisterSecondaryApi(TabCustomizationApi, "tabCustomizationApi");
        RegisterSecondaryApi(DomainCustomizationApi, "domainCustomizationApi");
    }
}
