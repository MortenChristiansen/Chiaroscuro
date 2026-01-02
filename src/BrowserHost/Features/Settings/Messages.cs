using BrowserHost.Utilities;

namespace BrowserHost.Features.Settings;

public record SaveSettingsCommand(SettingUiStateDto Settings) : ICommand;
public record SettingsSavedEvent(SettingUiStateDto Settings) : IEvent;

public record StartSsoFlowCommand(string TabId, string OriginalDomain, string OriginalUrl) : ICommand;
public record SsoFlowStartedEvent(string TabId, string OriginalDomain, string OriginalUrl) : IEvent;
