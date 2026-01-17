using BrowserHost.CefInfrastructure;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Features.TabPalette.LocalWebApp;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteBrowser : Browser
{
    public FindTextBackendApi FindTextApi { get; }
    public TabCustomizationBackendApi TabCustomizationApi { get; }
    public DomainCustomizationBackendApi DomainCustomizationApi { get; }
    public LocalWebAppBackendApi LocalWebAppApi { get; }

    public TabPaletteBrowser(PubSub pubSub)
        : base("/tab-palette", disableContextMenu: true)
    {
        FindTextApi = new FindTextBackendApi(pubSub);
        TabCustomizationApi = new TabCustomizationBackendApi(pubSub);
        DomainCustomizationApi = new DomainCustomizationBackendApi(pubSub);
        LocalWebAppApi = new LocalWebAppBackendApi(pubSub);

        RegisterSecondaryApi(FindTextApi, "findTextApi");
        RegisterSecondaryApi(TabCustomizationApi, "tabCustomizationApi");
        RegisterSecondaryApi(DomainCustomizationApi, "domainCustomizationApi");
        RegisterSecondaryApi(LocalWebAppApi, "localWebAppApi");
    }
}
