using BrowserHost.Features.CustomWindowChrome;

namespace BrowserHost.Tests.Fakes.WindowOperations;

internal class FakeCustomWindowChromeWindowOperations : CustomWindowChromeWindowOperations
{
    public bool EnableCalled { get; private set; }

    public override void EnableCustomWindowChrome(CustomWindowChromeBrowserApi customWindowChromeApi)
    {
        EnableCalled = true;
    }
}
