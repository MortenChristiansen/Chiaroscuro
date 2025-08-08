using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Settings;

public class SettingsFeature(MainWindow window) : Feature(window)
{
    private readonly SettingsBrowserApi _browserApi = new();
    private SettingsDataV1 _settings = SettingsStateManager.RestoreSettingsFromDisk();

    public override void Configure()
    {
        PubSub.Subscribe<TabBrowserCreatedEvent>(e =>
        {
            e.TabBrowser.RegisterContentPageApi(_browserApi, "settingsApi");
        });
        PubSub.Subscribe<SettingsPageLoadingEvent>(e =>
        {
            Window.CurrentTab?.SettingsLoaded(_settings);
        });
        PubSub.Subscribe<SettingsSavedEvent>(e =>
        {
            var mappedSettings = new SettingsDataV1(e.Settings.UserAgent);
            _settings = SettingsStateManager.SaveSettings(mappedSettings);
        });
    }
}
