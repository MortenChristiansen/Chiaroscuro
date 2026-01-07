using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Threading;

namespace BrowserHost.Features.Settings;

public class SettingsFeature(PubSub pubSub, SettingsStateManager settingsStateManager) : Feature(pubSub)
{
    private SettingsBackendApi _backendApi = null!;
    private readonly Lock _autoAddSsoLock = new();

    // These are the settings for the current execution, loaded from disk.
    public SettingsDataV1 ExecutionSettings { get; private set; } = settingsStateManager.RestoreSettingsFromDisk();

    public override void Configure()
    {
        _backendApi = new(PubSub, this);

        PubSub.Handle<SaveSettingsCommand>(cmd =>
        {
            var mappedSettings = new SettingsDataV1(cmd.Settings.UserAgent, cmd.Settings.SsoEnabledDomains, cmd.Settings.AutoAddSsoDomains);
            ExecutionSettings = settingsStateManager.SaveSettings(mappedSettings);
            PubSub.Publish(new SettingsSavedEvent(cmd.Settings));
        });
        PubSub.Handle<StartSsoFlowCommand>(cmd =>
        {
            var settings = ExecutionSettings;

            if (settings.AutoAddSsoDomains != true)
                return;

            lock (_autoAddSsoLock)
            {
                // Re-read the settings in case they changed while waiting for the lock
                settings = ExecutionSettings;

                if (settings.SsoEnabledDomains?.Contains(cmd.OriginalDomain, StringComparer.OrdinalIgnoreCase) == true)
                    return;

                PubSub.Send(new SaveSettingsCommand(new SettingUiStateDto(
                    settings.UserAgent,
                    [.. settings.SsoEnabledDomains ?? [], cmd.OriginalDomain],
                    AutoAddSsoDomains: true
                )));
            }

            PubSub.Publish(new SsoFlowStartedEvent(cmd.TabId, cmd.OriginalDomain, cmd.OriginalUrl));
        });

        PubSub.Subscribe<TabBrowserCreatedEvent>(e =>
        {
            if (ContentServer.IsSettingsPage(e.TabBrowser.Address))
                e.TabBrowser.RegisterContentPageApi(_backendApi, "settingsApi");
        });
    }
}
