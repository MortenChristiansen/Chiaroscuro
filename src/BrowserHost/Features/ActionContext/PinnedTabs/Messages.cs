using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public record PinTabCommand(string TabId) : ICommand;
public record TabPinnedEvent(string TabId) : IEvent;

public record UnpinTabCommand(string TabId) : ICommand;
public record TabUnpinnedEvent(string TabId) : IEvent;
