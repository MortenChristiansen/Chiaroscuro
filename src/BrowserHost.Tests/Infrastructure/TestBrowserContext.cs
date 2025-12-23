using BrowserHost.Tab;

namespace BrowserHost.Tests.Infrastructure;

internal class TestBrowserContext(ITabBrowser? tab = null) : IBrowserContext
{
    public ITabBrowser? CurrentTab { get; private set; } = tab;

    public void SetCurrentTab(ITabBrowser? tab)
    {
        CurrentTab = tab;
    }
}
