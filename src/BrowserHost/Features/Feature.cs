using CefSharp;
using CefSharp.Wpf;
using System.Collections.Generic;
using System.Diagnostics;

namespace BrowserHost.Features;

public abstract class Feature(MainWindow window, BrowserApi api)
{
    private readonly List<ChromiumWebBrowser> _browsers = [window.WebContent, window.ChromeUI, window.ActionDialog, window.Tabs];

    protected MainWindow Window { get; } = window;
    protected BrowserApi Api { get; } = api;

    public abstract void Register();

    protected void ConfigureUiControl(string name, string address, ChromiumWebBrowser uiComponent)
    {
        uiComponent.Address = ContentServer.GetUiAddress(address);
        uiComponent.JavascriptObjectRepository.Register("api", Api);
        uiComponent.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"{name}: {e.Message}");
        };
    }

    protected void RedrawBrowsers() =>
        _browsers.ForEach(b => b.GetBrowserHost()?.Invalidate(PaintElementType.View));
}
