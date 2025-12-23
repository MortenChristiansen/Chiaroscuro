using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Settings;

public record SettingsPageLoadingEvent();
public record SettingsSavedEvent(SettingUiStateDto Settings);
public record SsoFlowStartedEvent(string TabId, string OriginalDomain, string OriginalUrl);

public record SettingUiStateDto(string? UserAgent, string[] SsoEnabledDomains, bool AutoAddSsoDomains);

public class SettingsBrowserApi : BrowserApi
{
    public void SettingsPageLoading() =>
        PubSub.Instance.Publish(new SettingsPageLoadingEvent());

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

        var dto = new SettingUiStateDto(userAgent, ssoEnabledDomains, autoAddSsoDomains);
        PubSub.Instance.Publish(new SettingsSavedEvent(dto));
    }
}
