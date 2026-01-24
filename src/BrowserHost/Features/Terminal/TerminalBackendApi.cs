using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Terminal;

public class TerminalBackendApi(PubSub pubSub) : BackendApi
{
    public void ClearTerminal(string tabId)
    {
        pubSub.Send(new ClearTerminalCommand(tabId));
    }
}
