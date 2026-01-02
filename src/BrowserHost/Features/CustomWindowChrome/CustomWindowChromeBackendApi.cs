using BrowserHost.CefInfrastructure;
using BrowserHost.Logging;
using BrowserHost.Utilities;
using System.Windows;

namespace BrowserHost.Features.CustomWindowChrome;

public record MinimizeWindowCommand() : ICommand;
public record ToggleWindowStateCommand() : ICommand;
public record CopyAddressCommand() : ICommand;

public record WindowMinimizedEvent() : IEvent;
public record WindowStateToggledEvent() : IEvent;
public record AddressCopiedEvent() : IEvent;
public record TabLoadingStateChangedEvent(string TabId, bool IsLoading) : IEvent;

public class CustomWindowChromeBackendApi(PubSub pubSub) : BackendApi
{
    public bool CanGoForward() =>
        MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.CurrentTab?.CanGoForward ?? false);

    public void Forward() =>
        MainWindow.Instance.CurrentTab?.Forward();

    public bool CanGoBack() =>
        MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.CurrentTab?.CanGoBack ?? false);

    public void Back() =>
        MainWindow.Instance.CurrentTab?.Back();

    public void Reload() =>
        MainWindow.Instance.CurrentTab?.Reload();

    public void Minimize() =>
        pubSub.Send(new MinimizeWindowCommand());

    public void Maximize() =>
        pubSub.Send(new ToggleWindowStateCommand());

    public void Close() =>
        MainWindow.Instance.Dispatcher.Invoke(MainWindow.Instance.Close);

    public void CopyAddress() =>
        pubSub.Send(new CopyAddressCommand());

    public bool IsLoading() =>
        MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.CurrentTab?.IsLoading ?? false);

    public void OnLoaded() =>
        Measure.Event("Window chrome frontend loaded");

    public bool GetIsMaximized() =>
        MainWindow.Instance.Dispatcher.Invoke(() => MainWindow.Instance.WindowState == WindowState.Maximized);

    public DomainTrustRating? GetDomainTrustRating(string domain)
    {
        var rating = DomainTrustRatingProvider.LookupTrustpilotAsync(domain).GetAwaiter().GetResult();
        if (rating == null)
            return null;

        return rating;
    }
}
