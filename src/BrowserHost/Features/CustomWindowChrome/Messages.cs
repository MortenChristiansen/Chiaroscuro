using BrowserHost.Utilities;

namespace BrowserHost.Features.CustomWindowChrome;

public record MinimizeWindowCommand() : ICommand;
public record WindowMinimizedEvent() : IEvent;

public record ToggleWindowStateCommand() : ICommand;
public record WindowStateToggledEvent() : IEvent;

public record CopyAddressCommand() : ICommand;
public record AddressCopiedEvent() : IEvent;

public record TabLoadingStateChangedEvent(string TabId, bool IsLoading) : IEvent;
