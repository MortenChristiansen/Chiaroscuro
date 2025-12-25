using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Threading;

namespace BrowserHost.Features.Settings;

public class SettingsFeature(MainWindow window, SettingsBrowserApi settingsApi) : Feature(window)
{
    private readonly SettingsBackendApi _backendApi = new();
    private readonly Lock _autoAddSsoLock = new();

    // These are the settings for the current execution, loaded from disk.
    public static SettingsDataV1 ExecutionSettings { get; private set; } = SettingsStateManager.RestoreSettingsFromDisk();

    public override void Configure()
    {
        PubSub.Instance.Subscribe<TabBrowserCreatedEvent>(e =>
        {
            if (ContentServer.IsSettingsPage(e.TabBrowser.Address))
                e.TabBrowser.RegisterContentPageApi(_backendApi, "settingsApi");
        });
        PubSub.Instance.Subscribe<SettingsPageLoadingEvent>(e =>
        {
            var settings = ExecutionSettings;
            settingsApi.SettingsLoaded(new SettingUiStateDto(settings.UserAgent, settings.SsoEnabledDomains ?? [], settings.AutoAddSsoDomains ?? false));
        });
        PubSub.Instance.Subscribe<SettingsSavedEvent>(e =>
        {
            var mappedSettings = new SettingsDataV1(e.Settings.UserAgent, e.Settings.SsoEnabledDomains, e.Settings.AutoAddSsoDomains);
            ExecutionSettings = SettingsStateManager.SaveSettings(mappedSettings);
        });
        PubSub.Instance.Subscribe<SsoFlowStartedEvent>(e =>
        {
            var settings = ExecutionSettings;

            if (settings.AutoAddSsoDomains != true)
                return;

            lock (_autoAddSsoLock)
            {
                // Re-read the settings in case they changed while waiting for the lock
                settings = ExecutionSettings;

                if (settings.SsoEnabledDomains?.Contains(e.OriginalDomain, StringComparer.OrdinalIgnoreCase) == true)
                    return;

                PubSub.Instance.Publish(new SettingsSavedEvent(new SettingUiStateDto(
                    settings.UserAgent,
                    [.. settings.SsoEnabledDomains ?? [], e.OriginalDomain],
                    AutoAddSsoDomains: true
                )));
            }
        });
    }
}
