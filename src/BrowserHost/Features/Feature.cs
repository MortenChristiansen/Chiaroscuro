using BrowserHost.CefInfrastructure;
using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BrowserHost.Features;

public abstract class Feature<TApi>(MainWindow window, TApi api) : Feature(window) where TApi : BrowserApi
{
    public TApi Api { get; } = api;
}

public abstract class Feature(MainWindow window)
{
    protected MainWindow Window { get; } = window;

    public abstract void Register();

    public virtual bool HandleOnPreviewKeyDown(KeyEventArgs e) => false;

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
