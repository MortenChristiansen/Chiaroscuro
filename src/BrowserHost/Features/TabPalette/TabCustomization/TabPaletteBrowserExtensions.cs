using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public static class TabPaletteBrowserExtensions
{
    public static void InitCustomSettings(this TabPaletteBrowser browser, TabCustomizationDataV1 settings)
    {
        var settingsObject = new
        {
            customTitle = settings.CustomTitle,
            disableFixedAddress = settings.DisableFixedAddress,
            notificationPermission = (int)(settings.NotificationPermission ?? NotificationPermissionStatus.NotAsked)
        };
        browser.CallClientApi("initCustomSettings", settingsObject.ToJsonObject());
    }
}
