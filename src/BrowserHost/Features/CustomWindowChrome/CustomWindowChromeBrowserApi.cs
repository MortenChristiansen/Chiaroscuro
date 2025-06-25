using BrowserHost.CefInfrastructure;
using CefSharp;
using System.Threading.Channels;

namespace BrowserHost.Features.CustomWindowChrome;

public record WindowMinimizedEvent();
public record WindowStateToggledEvent();

public class CustomWindowChromeBrowserApi(CustomWindowChromeBrowser browser) : BrowserApi(browser)
{
    public Channel<WindowMinimizedEvent> WindowMinimizedChannel { get; } = Channel.CreateUnbounded<WindowMinimizedEvent>();
    public Channel<WindowStateToggledEvent> WindowStateToggledChannel { get; } = Channel.CreateUnbounded<WindowStateToggledEvent>();

    // TODO: Is there a better way to get the MainWindow instance?

    public bool CanGoForward() =>
        MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.CurrentTab?.CanGoForward ?? false);

    public void Forward() =>
        MainWindow.Instance.CurrentTab.Forward();

    public bool CanGoBack() =>
        MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.CurrentTab?.CanGoBack ?? false);

    public void Back() =>
        MainWindow.Instance.CurrentTab.Back();

    public void Reload() =>
        MainWindow.Instance.CurrentTab?.Reload();

    public void Minimize() =>
        WindowMinimizedChannel.Writer.TryWrite(new WindowMinimizedEvent());

    public void Maximize() =>
        WindowStateToggledChannel.Writer.TryWrite(new WindowStateToggledEvent());

    public void Close() =>
        MainWindow.Instance.Dispatcher.Invoke(MainWindow.Instance.Close);
}
