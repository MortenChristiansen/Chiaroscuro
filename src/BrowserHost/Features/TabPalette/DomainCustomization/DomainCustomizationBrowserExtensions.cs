using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public static class DomainCustomizationBrowserExtensions
{
    public static void InitDomainSettings(this TabPaletteBrowser browser, string domain, bool cssEnabled, bool hasCustomCss)
    {
        var args = $"{domain.ToJsonString()}, {cssEnabled.ToJsonBoolean()}, {hasCustomCss.ToJsonBoolean()}";
        browser.CallClientApi("initDomainSettings", args);
    }

    public static void UpdateDomainSettings(this TabPaletteBrowser browser, string domain, bool cssEnabled, bool hasCustomCss)
    {
        var args = $"{domain.ToJsonString()}, {cssEnabled.ToJsonBoolean()}, {hasCustomCss.ToJsonBoolean()}";
        browser.CallClientApi("updateDomainSettings", args);
    }
}