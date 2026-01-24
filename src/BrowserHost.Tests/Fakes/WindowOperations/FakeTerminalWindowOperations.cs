using BrowserHost.Features.Terminal;

namespace BrowserHost.Tests.Fakes.WindowOperations;

internal class FakeTerminalWindowOperations : TerminalWindowOperations
{
    public bool IsTerminalVisibleValue { get; private set; }
    public int ShowTerminalCallCount { get; private set; }
    public int HideTerminalCallCount { get; private set; }

    public override bool IsTerminalVisible => IsTerminalVisibleValue;

    public override void ShowTerminal()
    {
        ShowTerminalCallCount++;
        IsTerminalVisibleValue = true;
    }

    public override void HideTerminal()
    {
        HideTerminalCallCount++;
        IsTerminalVisibleValue = false;
    }
}
