using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.FindText;

public record FindTextCommand(string Term) : ICommand;
public record FindTextTriggeredEvent(string Term) : IEvent;

public record FindNextTextMatchCommand(string Term) : ICommand;
public record NextTextMatchTriggeredEvent(string Term) : IEvent;

public record FindPrevTextMatchCommand(string Term) : ICommand;
public record PrevTextMatchTriggeredEvent(string Term) : IEvent;

public record StopFindingTextCommand() : ICommand;
public record StopFindingTextTriggeredEvent() : IEvent;

public record ChangeFindStatusCommand(int Matches) : ICommand;
public record FindStatusChangedEvent(int Matches) : IEvent;
