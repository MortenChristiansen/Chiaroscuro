using BrowserHost.Tab;

namespace BrowserHost;

public class BrowserContext(MainWindow window) : IBrowserContext
{
    public ITabBrowser? CurrentTab { get; private set; } = window.CurrentTab;
}
