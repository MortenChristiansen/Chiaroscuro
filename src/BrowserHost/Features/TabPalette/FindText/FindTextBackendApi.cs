using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.FindText;

public record FindTextEvent(string Term);
public record NextTextMatchEvent(string Term);
public record PrevTextMatchEvent(string Term);
public record StopFindingTextEvent();
public record FindStatusChangedEvent(int Matches);

public class FindTextBackendApi(PubSub pubSub) : BackendApi
{
    public void Find(string term) =>
        pubSub.Publish(new FindTextEvent(term));

    public void NextMatch(string term) =>
        pubSub.Publish(new NextTextMatchEvent(term));

    public void PrevMatch(string term) =>
        pubSub.Publish(new PrevTextMatchEvent(term));

    public void StopFinding() =>
        pubSub.Publish(new StopFindingTextEvent());
}
