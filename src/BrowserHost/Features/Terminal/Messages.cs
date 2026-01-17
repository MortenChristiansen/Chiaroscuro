using BrowserHost.Utilities;

namespace BrowserHost.Features.Terminal;

public record ToggleTerminalCommand() : ICommand;
public record ClearTerminalCommand(string TabId) : ICommand;

public record TerminalToggledEvent(bool IsVisible) : IEvent;
public record TerminalOutputEvent(string TabId, string Output, bool IsError) : IEvent;
