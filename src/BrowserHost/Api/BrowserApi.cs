using BrowserHost.Api.Dtos;
using CefSharp;
using System.Threading.Channels;

namespace BrowserHost.Api;

public record ActionDialogDismissedEvent();
public record WindowMinimizedEvent();
public record WindowStateToggledEvent();
public record NavigationStartedEvent(string Address);

public class BrowserApi(MainWindow window)
{
    public Channel<ActionDialogDismissedEvent> ActionDialogDismissedChannel { get; } = Channel.CreateUnbounded<ActionDialogDismissedEvent>();
    public Channel<WindowMinimizedEvent> WindowMinimizedChannel { get; } = Channel.CreateUnbounded<WindowMinimizedEvent>();
    public Channel<WindowStateToggledEvent> WindowStateToggledChannel { get; } = Channel.CreateUnbounded<WindowStateToggledEvent>();
    public Channel<NavigationStartedEvent> NavigationStartedChannel { get; } = Channel.CreateUnbounded<NavigationStartedEvent>();

    public void ChangeAddress(string address) =>
        window.ChromeUI.ExecuteScriptAsync($"window.angularApi.changeAddress('{address}')");

    public bool CanGoForward() =>
        window.Dispatcher.Invoke(() => window.CurrentTab.CanGoForward);

    public void Forward() =>
        window.CurrentTab.Forward();

    public bool CanGoBack() =>
        window.Dispatcher.Invoke(() => window.CurrentTab.CanGoBack);

    public void Back() =>
        window.CurrentTab.Back();

    public void Navigate(string url) =>
        NavigationStartedChannel.Writer.TryWrite(new NavigationStartedEvent(url));

    public void Reload() =>
        window.WebContent.Reload();

    public void DismissActionDialog() =>
        ActionDialogDismissedChannel.Writer.TryWrite(new ActionDialogDismissedEvent());

    public void Minimize() =>
        WindowMinimizedChannel.Writer.TryWrite(new WindowMinimizedEvent());

    public void Maximize() =>
        WindowStateToggledChannel.Writer.TryWrite(new WindowStateToggledEvent());

    public void Close() =>
        window.Dispatcher.Invoke(window.Close);

    public void AddTab(TabDto tab, bool activate = true)
    {
        var tabJson = System.Text.Json.JsonSerializer.Serialize(tab);
        window.ChromeUI.ExecuteScriptAsync($"window.angularApi.addTab({tabJson}, {activate})");
    }
}
