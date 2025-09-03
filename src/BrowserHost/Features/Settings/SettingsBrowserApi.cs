using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Settings;

public record SettingsPageLoadingEvent();
public record SettingsSavedEvent(SettingUiStateDto Settings);

public record SettingUiStateDto(string? UserAgent, string[] SsoEnabledDomains);

public class SettingsBrowserApi : BrowserApi
{
    public void SettingsPageLoading() =>
        PubSub.Publish(new SettingsPageLoadingEvent());

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

        var dto = new SettingUiStateDto(userAgent, ssoEnabledDomains);
        PubSub.Publish(new SettingsSavedEvent(dto));
    }
}
