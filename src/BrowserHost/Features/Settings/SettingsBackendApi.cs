using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Settings;

public record SettingUiStateDto(string? UserAgent, string[] SsoEnabledDomains, bool AutoAddSsoDomains, bool EnableGpuCompositing);

public class SettingsBackendApi(PubSub pubSub, SettingsFeature settingsFeature) : BackendApi
{
    public SettingUiStateDto LoadSettingsPage()
    {
        var settings = settingsFeature.ExecutionSettings;
        return new SettingUiStateDto(settings.UserAgent, settings.SsoEnabledDomains ?? [], settings.AutoAddSsoDomains ?? false, settings.EnableGpuCompositing ?? false);
    }

    public void SaveSettings(IDictionary<string, object?> settings)
    {
        var userAgent = settings.TryGetValue("userAgent", out var o) && o is string ua ? ua : null;
        var ssoEnabledDomains = settings.TryGetValue("ssoEnabledDomains", out var o2) && o2 is IEnumerable<object> list
            ? list
                .OfType<string>()
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : [];
        var autoAddSsoDomains = settings.TryGetValue("autoAddSsoDomains", out var o3) && o3 is bool b ? b : false;

        var enableGpuCompositing = settings.TryGetValue("enableGpuCompositing", out var o4) && o4 is bool b2 ? b2 : false;

        var dto = new SettingUiStateDto(userAgent, ssoEnabledDomains, autoAddSsoDomains, enableGpuCompositing);
        pubSub.Send(new SaveSettingsCommand(dto));
    }
}
