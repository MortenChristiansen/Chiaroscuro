using BrowserHost.Api;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;

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

    protected async Task Listen<TEvent>(Channel<TEvent> channel, Action<TEvent> action, bool dispatchToUi = false)
    {
        var reader = channel.Reader;
        while (await reader.WaitToReadAsync())
        {
            while (reader.TryRead(out var evt))
            {
                if (dispatchToUi)
                {
                    Window.Dispatcher.Invoke(() => action(evt));
                }
                else
                {
                    action(evt);
                }
            }
        }
    }
}
