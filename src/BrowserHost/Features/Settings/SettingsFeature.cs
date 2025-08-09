using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Settings;

public class SettingsFeature(MainWindow window) : Feature(window)
{
    private readonly SettingsBrowserApi _browserApi = new();

    // These are the settings for the current execution, loaded from disk.
    // They are only loaded on startup.
    public static SettingsDataV1 ExecutionSettings { get; } = SettingsStateManager.RestoreSettingsFromDisk();

    private SettingsDataV1 _settings = ExecutionSettings;

    public override void Configure()
    {
        PubSub.Subscribe<TabBrowserCreatedEvent>(e =>
        {
            if (ContentServer.IsSettingsPage(e.TabBrowser.Address))
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
