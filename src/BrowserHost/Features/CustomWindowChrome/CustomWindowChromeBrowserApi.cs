using BrowserHost.CefInfrastructure;
using CefSharp;
using BrowserHost.Utilities;

namespace BrowserHost.Features.CustomWindowChrome;

public record WindowMinimizedEvent();
public record WindowStateToggledEvent();
public record AddressCopyRequestedEvent();

public class CustomWindowChromeBrowserApi(CustomWindowChromeBrowser browser) : BrowserApi(browser)
{
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
        PubSub.Publish(new WindowMinimizedEvent());

    public void Maximize() =>
        PubSub.Publish(new WindowStateToggledEvent());

    public void Close() =>
        MainWindow.Instance.Dispatcher.Invoke(MainWindow.Instance.Close);

    public void CopyAddress() =>
        PubSub.Publish(new AddressCopyRequestedEvent());
}
