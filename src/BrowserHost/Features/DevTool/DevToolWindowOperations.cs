using CefSharp;

namespace BrowserHost.Features.DevTool;

public class DevToolWindowOperations
{
    private MainWindow _window = null!;

    protected DevToolWindowOperations() { }

    public DevToolWindowOperations(MainWindow window)
    {
        _window = window;
    }

    public virtual void ToggleActionContextDevTools() => ToggleDevTools(_window.ActionContext.GetBrowserHost());

    public virtual void ToggleTabPaletteDevTools() => ToggleDevTools(_window.TabPaletteBrowserControl.GetBrowserHost());

    private static void ToggleDevTools(IBrowserHost? browserHost)
    {
        if (browserHost != null)
        {
            if (browserHost.HasDevTools)
                browserHost.CloseDevTools();
            else
                browserHost.ShowDevTools();
        }
    }
}
