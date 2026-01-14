using BrowserHost.Features.DevTool;

namespace BrowserHost.Tests.Fakes.WindowOperations;

internal class FakeDevToolWindowOperations : DevToolWindowOperations
{
    public int ToggleActionContextDevToolsCallCount { get; private set; }
    public int ToggleTabPaletteDevToolsCallCount { get; private set; }

    public override void ToggleActionContextDevTools() => ToggleActionContextDevToolsCallCount++;

    public override void ToggleTabPaletteDevTools() => ToggleTabPaletteDevToolsCallCount++;
}
