using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.FindText;

public record FindTextCommand(string Term) : ICommand;
public record FindNextTextMatchCommand(string Term) : ICommand;
public record FindPrevTextMatchCommand(string Term) : ICommand;
public record StopFindingTextCommand() : ICommand;
public record ChangeFindStatusCommand(int Matches) : ICommand;

public record FindTextTriggeredEvent(string Term) : IEvent;
public record NextTextMatchTriggeredEvent(string Term) : IEvent;
public record PrevTextMatchTriggeredEvent(string Term) : IEvent;
public record StopFindingTextTriggeredEvent() : IEvent;
public record FindStatusChangedEvent(int Matches) : IEvent;

public class FindTextBackendApi(PubSub pubSub) : BackendApi
{
    public void Find(string term) =>
        pubSub.Send(new FindTextCommand(term));

    public void NextMatch(string term) =>
        pubSub.Send(new FindNextTextMatchCommand(term));

    public void PrevMatch(string term) =>
        pubSub.Send(new FindPrevTextMatchCommand(term));

    public void StopFinding() =>
        pubSub.Send(new StopFindingTextCommand());
}
