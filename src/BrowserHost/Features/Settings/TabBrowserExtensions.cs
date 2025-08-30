using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Settings;

public static class TabBrowserExtensions
{
    public static void SettingsLoaded(this TabBrowser browser, SettingUiStateDto settings) =>
        browser.CallClientApi("settingsLoaded", settings.ToJsonObject());
}
