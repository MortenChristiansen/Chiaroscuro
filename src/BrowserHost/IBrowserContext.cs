using BrowserHost.Tab;

namespace BrowserHost;

public interface IBrowserContext
{
    public ITabBrowser? CurrentTab { get; }
}
