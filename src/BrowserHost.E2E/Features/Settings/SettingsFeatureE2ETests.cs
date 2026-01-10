using BrowserHost.E2E.Infrastructure;
using BrowserHost.Features.Settings;

namespace BrowserHost.E2E.Features.Settings;

public class SettingsFeatureE2ETests
{
    [Fact]
    public Task Open_settings_via_action_dialog_command() =>
        StaTestRunner.RunAsync(async () =>
        {
            using var host = E2EApplicationHost.Create();

            await host.PressCtrlTAsync();
            await host.TypeInActionDialogAsync("/settings");
            await host.PressEnterInActionDialogAsync();

            await host.WaitForSettingsPageReadyAsync();
        });

    [Fact]
    public Task Save_user_agent_from_settings_page() =>
        StaTestRunner.RunAsync(async () =>
        {
            using var host = E2ETabBrowserHost.Create();

            await host.OpenSettingsPageAsync();

            await host.SetTextSettingAsync("User Agent", "E2E-UA/1.0");
            await host.ClickButtonByTextAsync("Save");

            await host.WaitUntilAsync(() => host.Settings.ExecutionSettings.UserAgent == "E2E-UA/1.0");
            Assert.Equal("E2E-UA/1.0", host.Settings.ExecutionSettings.UserAgent);
        });

    [Fact]
    public Task Save_sso_settings_from_settings_page() =>
        StaTestRunner.RunAsync(async () =>
        {
            using var host = E2ETabBrowserHost.Create();

            await host.OpenSettingsPageAsync();

            await host.SetCheckboxSettingAsync("Auto Add SSO Domains", checkedValue: true);

            // Add duplicates + whitespace to validate SettingsBackendApi normalization.
            await host.AddStringArrayItemAsync("SSO Enabled Domains", " example.com ");
            await host.AddStringArrayItemAsync("SSO Enabled Domains", "EXAMPLE.com");

            await host.ClickButtonByTextAsync("Save");

            await host.WaitUntilAsync(() => host.Settings.ExecutionSettings.AutoAddSsoDomains == true);

            Assert.True(host.Settings.ExecutionSettings.AutoAddSsoDomains);
            Assert.NotNull(host.Settings.ExecutionSettings.SsoEnabledDomains);
            Assert.Equal(["example.com"], host.Settings.ExecutionSettings.SsoEnabledDomains!);
        });

    [Fact]
    public Task Auto_adds_domain_when_sso_flow_started_if_enabled() =>
        StaTestRunner.RunAsync(async () =>
        {
            using var host = E2ETabBrowserHost.Create();

            await host.OpenSettingsPageAsync();

            await host.SetCheckboxSettingAsync("Auto Add SSO Domains", checkedValue: true);
            await host.ClickButtonByTextAsync("Save");

            await host.WaitUntilAsync(() => host.Settings.ExecutionSettings.AutoAddSsoDomains == true);

            // Simulate the SSO flow being detected; SettingsFeature should auto-add the domain.
            host.PubSub.Send(new StartSsoFlowCommand(host.Tab.Id, "contoso.com", "https://contoso.com/login"));

            await host.WaitUntilAsync(() => host.Settings.ExecutionSettings.SsoEnabledDomains?.Contains("contoso.com", StringComparer.OrdinalIgnoreCase) == true);
            Assert.Contains("contoso.com", host.Settings.ExecutionSettings.SsoEnabledDomains!, StringComparer.OrdinalIgnoreCase);
        });
}
