using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette;

public record FindTextEvent(string Term);
public record NextTextMatchEvent(string Term);
public record PrevTextMatchEvent(string Term);
public record StopFindingTextEvent();
public record FindStatusChangedEvent(int Matches);

public class TabPaletteBrowserApi : BrowserApi
{
    public void Find(string term) =>
        PubSub.Publish(new FindTextEvent(term));

    public void NextMatch(string term) =>
        PubSub.Publish(new NextTextMatchEvent(term));

    public void PrevMatch(string term) =>
        PubSub.Publish(new PrevTextMatchEvent(term));

    public void StopFinding() =>
        PubSub.Publish(new StopFindingTextEvent());
}
