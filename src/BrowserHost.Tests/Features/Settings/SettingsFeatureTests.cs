using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.Settings;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System.Text.Json;

namespace BrowserHost.Tests.Features.Settings;

public class SettingsFeatureTests
{
    [Fact]
    public void Configuring_the_feature_restores_execution_settings_from_the_state_manager()
    {
        var feature = CreateFeature
            .ConfigureContext(c => SeedSettings(c, new("UA", ["sso.example"], true)))
            .BuildSettingsFeature();

        Assert.Equal("UA", feature.ExecutionSettings.UserAgent);
        Assert.Equal(["sso.example"], feature.ExecutionSettings.SsoEnabledDomains!);
        Assert.True(feature.ExecutionSettings.AutoAddSsoDomains);
    }

    [Fact]
    public void Publishing_a_TabBrowserCreatedEvent_registers_the_settings_backend_api_for_the_settings_page()
    {
        CreateFeature
            .WithCurrentTab(out var tab, address: "/settings")
            .CaptureContext(out var context)
            .BuildSettingsFeature();

        context.PubSub.Publish(new TabBrowserCreatedEvent(tab));

        var registration = Assert.Single(tab.RegisteredContentPageApis);
        Assert.Equal("settingsApi", registration.Name);
        Assert.IsType<SettingsBackendApi>(registration.Api);
    }

    [Fact]
    public void Publishing_a_TabBrowserCreatedEvent_does_not_register_the_settings_api_for_non_settings_pages()
    {
        CreateFeature
            .WithCurrentTab(out var tab, address: "https://other.com/settings")
            .CaptureContext(out var context)
            .BuildSettingsFeature();

        context.PubSub.Publish(new TabBrowserCreatedEvent(tab));

        Assert.Empty(tab.RegisteredContentPageApis);
    }

    [Fact]
    public void Sending_a_SaveSettingsCommand_persists_the_settings_and_updates_the_execution_settings()
    {
        var feature = CreateFeature
            .ConfigureContext(c =>
            {
                SeedSettings(c, new("before", ["before.com"], false));
            })
            .CaptureContext(out var context)
            .BuildSettingsFeature();

        context.PubSub.Send(new SaveSettingsCommand(new SettingUiStateDto("UA2", ["a.com"], true)));

        Assert.Equal("UA2", feature.ExecutionSettings.UserAgent);
        Assert.Equal(["a.com"], feature.ExecutionSettings.SsoEnabledDomains!);
        Assert.True(feature.ExecutionSettings.AutoAddSsoDomains);
        var restored = new SettingsStateManager(context.FileSystem).RestoreSettingsFromDisk();
        Assert.Equal("UA2", restored.UserAgent);
        Assert.Equal(["a.com"], restored.SsoEnabledDomains!);
        Assert.True(restored.AutoAddSsoDomains);
    }

    [Fact]
    public void Sending_a_StartSsoFlowCommand_does_not_publish_anything_when_auto_add_is_disabled()
    {
        CreateFeature
            .ConfigureContext(c => SeedSettings(c, new("UA", [], AutoAddSsoDomains: false)))
            .CaptureContext(out var context)
            .BuildSettingsFeature();

        context.PubSub.Send(new StartSsoFlowCommand("tab-1", "example.com", "https://example.com/"));

        Assert.Empty(PubSubMessages.OfType<SettingsSavedEvent>());
    }

    [Fact]
    public void Sending_a_StartSsoFlowCommand_publishes_a_SettingsSavedEvent_with_the_domain_added_when_auto_add_is_enabled()
    {
        CreateFeature
            .ConfigureContext(c => SeedSettings(c, new("UA", [], AutoAddSsoDomains: true)))
            .CaptureContext(out var context)
            .BuildSettingsFeature();

        context.PubSub.Send(new StartSsoFlowCommand("tab-1", "example.com", "https://example.com/"));

        var saved = Assert.Single(PubSubMessages.OfType<SettingsSavedEvent>());
        Assert.Equal(["example.com"], saved.Settings.SsoEnabledDomains);
        Assert.True(saved.Settings.AutoAddSsoDomains);
    }

    [Fact]
    public void Sending_a_StartSsoFlowCommand_does_not_publish_a_SettingsSavedEvent_when_the_domain_is_already_enabled()
    {
        CreateFeature
            .ConfigureContext(c => SeedSettings(c, new("UA", ["example.com"], AutoAddSsoDomains: true)))
            .CaptureContext(out var context)
            .BuildSettingsFeature();

        context.PubSub.Send(new StartSsoFlowCommand("tab-1", "example.com", "https://example.com/"));

        Assert.Empty(PubSubMessages.OfType<SettingsSavedEvent>());
    }

    private static void SeedSettings(TestBrowserContext context, SettingsDataV1 settings)
    {
        var settingsPath = SettingsStateManager.PersistedStatePath;
        var settingsFolder = context.FileSystem.Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(settingsFolder))
        {
            context.FileSystem.Directory.CreateDirectory(settingsFolder);
        }

        var versioned = new PersistentData<SettingsDataV1>
        {
            Version = 1,
            Data = settings,
        };

        context.FileSystem.File.WriteAllText(settingsPath, JsonSerializer.Serialize(versioned, BrowserHostJsonContext.Default.PersistentDataSettingsDataV1));
    }
}
