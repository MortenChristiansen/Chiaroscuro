using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser : Browser
{
    public FindTextBackendApi FindTextApi { get; } = new(App.PubSub);
    public TabCustomizationBackendApi TabCustomizationApi { get; } = new(App.PubSub);
    public DomainCustomizationBackendApi DomainCustomizationApi { get; } = new(App.PubSub);

    public TabPaletteBrowser()
        : base("/tab-palette", disableContextMenu: true)
    {
        RegisterSecondaryApi(FindTextApi, "findTextApi");
        RegisterSecondaryApi(TabCustomizationApi, "tabCustomizationApi");
        RegisterSecondaryApi(DomainCustomizationApi, "domainCustomizationApi");
    }
}
