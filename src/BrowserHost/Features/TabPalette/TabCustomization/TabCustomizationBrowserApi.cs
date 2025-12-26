using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public class TabCustomizationBrowserApi(BaseBrowser tabPaletteBrowser) : BrowserApi(tabPaletteBrowser)
{
    public void InitCustomSettings(TabCustomizationDataV1 settings) =>
        CallClientApi("initCustomSettings", settings.ToJsonObject());

    public void SetTabCustomizations(TabCustomizationDto[] customizations) =>
        CallClientApi("setTabCustomizations", customizations.ToJsonObject());

    public void UpdateTabCustomization(TabCustomizationDto customization) =>
        CallClientApi("updateTabCustomization", customization.ToJsonObject());
}
