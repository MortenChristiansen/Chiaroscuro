using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Terminal;

public class TerminalBrowser : Browser
{
    public TerminalBackendApi TerminalApi { get; }

    public TerminalBrowser(PubSub pubSub)
        : base("/terminal", disableContextMenu: true)
    {
        TerminalApi = new TerminalBackendApi(pubSub);
        RegisterSecondaryApi(TerminalApi, "terminalApi");
    }
}
