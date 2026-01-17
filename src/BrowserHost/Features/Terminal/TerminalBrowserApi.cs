using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.Terminal;

public class TerminalBrowserApi(BaseBrowser terminalBrowser) : BrowserApi(terminalBrowser)
{
    public void WriteOutput(string tabId, string output, bool isError)
    {
        var args = $"{tabId.ToJsonString()}, {output.ToJsonString()}, {isError.ToJsonBoolean()}";
        CallClientApi("writeTerminalOutput", args);
    }

    public void InitTerminal(string tabId)
    {
        CallClientApi("initTerminal", tabId.ToJsonString());
    }

    public void ClearTerminal(string tabId)
    {
        CallClientApi("clearTerminal", tabId.ToJsonString());
    }

    public void SetVisibility(bool visible)
    {
        CallClientApi("setTerminalVisibility", visible.ToJsonBoolean());
    }
}
