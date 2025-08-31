using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Settings;

public record SettingsPageLoadingEvent();
public record SettingsSavedEvent(SettingUiStateDto Settings);
public record ApplicationSettingsChangedEvent(SettingsDataV1 Settings);

public record SettingUiStateDto(string? UserAgent, string[] SsoEnabledDomains);

public class SettingsBrowserApi : BrowserApi
{
    public void SettingsPageLoading() =>
        PubSub.Publish(new SettingsPageLoadingEvent());

    public void SaveSettings(IDictionary<string, object?> settings)
    {
        var userAgent = settings.TryGetValue("userAgent", out var o) && o is string ua ? ua : null;
        var ssoEnabledDomains = settings.TryGetValue("ssoEnabledDomains", out var o2) && o2 is IEnumerable<object> list
            ? list.Cast<string>().ToArray()
            : [];

        var dto = new SettingUiStateDto(userAgent, ssoEnabledDomains);
        PubSub.Publish(new SettingsSavedEvent(dto));
    }
}
