using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Threading;

namespace BrowserHost.Features.Settings;

public class SettingsFeature(MainWindow window, PubSub pubSub, SettingsBrowserApi settingsApi, SettingsStateManager settingsStateManager) : Feature(window, pubSub)
{
    private readonly SettingsBackendApi _backendApi = new(pubSub);
    private readonly Lock _autoAddSsoLock = new();

    // These are the settings for the current execution, loaded from disk.
    public SettingsDataV1 ExecutionSettings { get; private set; } = settingsStateManager.RestoreSettingsFromDisk();

    public override void Configure()
    {
        PubSub.Handle<SetSettingsPageLoadingStateCommand>(_ =>
        {
            var settings = ExecutionSettings;
            settingsApi.SettingsLoaded(new SettingUiStateDto(settings.UserAgent, settings.SsoEnabledDomains ?? [], settings.AutoAddSsoDomains ?? false));
            PubSub.Publish(new SettingsPageLoadingEvent());
        });
        PubSub.Handle<SaveSettingsCommand>(e =>
        {
            var mappedSettings = new SettingsDataV1(e.Settings.UserAgent, e.Settings.SsoEnabledDomains, e.Settings.AutoAddSsoDomains);
            ExecutionSettings = settingsStateManager.SaveSettings(mappedSettings);
            PubSub.Publish(new SettingsSavedEvent(e.Settings));
        });
        PubSub.Handle<StartSsoFlowCommand>(e =>
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

                PubSub.Send(new SaveSettingsCommand(new SettingUiStateDto(
                    settings.UserAgent,
                    [.. settings.SsoEnabledDomains ?? [], e.OriginalDomain],
                    AutoAddSsoDomains: true
                )));
            }

            PubSub.Publish(new SsoFlowStartedEvent(e.TabId, e.OriginalDomain, e.OriginalUrl));
        });

        PubSub.Subscribe<TabBrowserCreatedEvent>(e =>
        {
            if (ContentServer.IsSettingsPage(e.TabBrowser.Address))
                e.TabBrowser.RegisterContentPageApi(_backendApi, "settingsApi");
        });
    }
}
