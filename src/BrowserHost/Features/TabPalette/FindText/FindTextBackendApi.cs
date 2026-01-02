using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.FindText;

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
