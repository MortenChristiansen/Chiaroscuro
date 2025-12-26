using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.Settings;

public class SettingsBrowserApi(Func<TabBrowser?> getCurrentTabBrowser)
{
    public void SettingsLoaded(SettingUiStateDto settings) =>
        getCurrentTabBrowser()?.CallClientApi("settingsLoaded", settings.ToJsonObject());
}
