using BrowserHost.Api.Dtos;
using CefSharp;
using System;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BrowserHost.Api;

public record ActionDialogDismissedEvent();
public record WindowMinimizedEvent();
public record WindowStateToggledEvent();
public record NavigationStartedEvent(string Address);

[Flags]
public enum UiSources
{
    None = 0,
    Tabs = 1,
    WindowChrome = 2,
    ActionDialog = 4
}

public class BrowserApi(MainWindow window)
{
    private UiSources _loadedSources = UiSources.None;

    public Channel<ActionDialogDismissedEvent> ActionDialogDismissedChannel { get; } = Channel.CreateUnbounded<ActionDialogDismissedEvent>();
    public Channel<WindowMinimizedEvent> WindowMinimizedChannel { get; } = Channel.CreateUnbounded<WindowMinimizedEvent>();
    public Channel<WindowStateToggledEvent> WindowStateToggledChannel { get; } = Channel.CreateUnbounded<WindowStateToggledEvent>();
    public Channel<NavigationStartedEvent> NavigationStartedChannel { get; } = Channel.CreateUnbounded<NavigationStartedEvent>();

    public void ChangeAddress(string address) =>
        window.ChromeUI.ExecuteScriptAsync($"window.angularApi.changeAddress('{address}')");

    public bool CanGoForward() =>
        window.Dispatcher.Invoke(() => window.CurrentTab?.CanGoForward ?? false);

    public void Forward() =>
        window.CurrentTab.Forward();

    public bool CanGoBack() =>
        window.Dispatcher.Invoke(() => window.CurrentTab?.CanGoBack ?? false);

    public void Back() =>
        window.CurrentTab.Back();

    public void Navigate(string url) =>
        NavigationStartedChannel.Writer.TryWrite(new NavigationStartedEvent(url));

    public void Reload() =>
        window.CurrentTab?.Reload();

    public void DismissActionDialog() =>
        ActionDialogDismissedChannel.Writer.TryWrite(new ActionDialogDismissedEvent());

    public void Minimize() =>
        WindowMinimizedChannel.Writer.TryWrite(new WindowMinimizedEvent());

    public void Maximize() =>
        WindowStateToggledChannel.Writer.TryWrite(new WindowStateToggledEvent());

    public void Close() =>
        window.Dispatcher.Invoke(window.Close);

    public void UiLoaded(string source)
    {
        var sourceEnum = Enum.TryParse<UiSources>(source, out var parsedSource) ? parsedSource : UiSources.None;
        _loadedSources |= sourceEnum;
    }

    private static readonly JsonSerializerOptions _tabSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public void AddTab(TabDto tab, bool activate = true)
    {
        RunWhenSourceHasLoaded(UiSources.Tabs, () =>
        {
            var tabJson = JsonSerializer.Serialize(tab, _tabSerializationOptions);
            var script = $"window.angularApi.addTab({tabJson}, {(activate ? "true" : "false")})";
            window.Tabs.ExecuteScriptAsync(script);
        });
    }

    public void UpdateTab(TabDto tab)
    {
        RunWhenSourceHasLoaded(UiSources.Tabs, () =>
        {
            var tabJson = JsonSerializer.Serialize(tab, _tabSerializationOptions);
            window.Tabs.ExecuteScriptAsync($"window.angularApi.updateTab({tabJson})");
        });
    }

    private void RunWhenSourceHasLoaded(UiSources source, Action action)
    {
        if (_loadedSources.HasFlag(source))
        {
            action();
        }
        else
        {
            Task.Run(async () =>
            {
                while (!_loadedSources.HasFlag(source))
                {
                    await Task.Delay(100); // Wait until the source is loaded
                }
                window.Dispatcher.Invoke(action);
            });
        }

    }
}
