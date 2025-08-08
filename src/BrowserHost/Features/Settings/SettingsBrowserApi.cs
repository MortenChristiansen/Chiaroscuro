using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Settings;

public record SettingsPageLoadingEvent();
public record SettingsSavedEvent(SettingUiStateDto Settings);

public record SettingUiStateDto(string? UserAgent);

public class SettingsBrowserApi : BrowserApi
{
    public void SettingsPageLoading() =>
        PubSub.Publish(new SettingsPageLoadingEvent());

    public void SaveSettings(dynamic settings)
    {
        var dto = new SettingUiStateDto(settings.userAgent);
        PubSub.Publish(new SettingsSavedEvent(dto));
    }
}
