using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public class DomainCustomizationBrowserApi(BaseBrowser tabPaletteBrowser) : BrowserApi(tabPaletteBrowser)
{
    public void InitDomainSettings(string domain, bool cssEnabled, bool hasCustomCss)
    {
        var args = $"{domain.ToJsonString()}, {cssEnabled.ToJsonBoolean()}, {hasCustomCss.ToJsonBoolean()}";
        CallClientApi("initDomainSettings", args);
    }

    public void UpdateDomainSettings(string domain, bool cssEnabled, bool hasCustomCss)
    {
        var args = $"{domain.ToJsonString()}, {cssEnabled.ToJsonBoolean()}, {hasCustomCss.ToJsonBoolean()}";
        CallClientApi("updateDomainSettings", args);
    }
}