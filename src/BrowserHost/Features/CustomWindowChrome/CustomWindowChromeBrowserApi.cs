using BrowserHost.CefInfrastructure;
using BrowserHost.Logging;
using BrowserHost.Utilities;
using System.Windows;

namespace BrowserHost.Features.CustomWindowChrome;

public record WindowMinimizedEvent();
public record WindowStateToggledEvent();
public record AddressCopyRequestedEvent();
public record TabLoadingStateChangedEvent(string TabId, bool IsLoading);

public class CustomWindowChromeBrowserApi : BrowserApi
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
        PubSub.Publish(new WindowMinimizedEvent());

    public void Maximize() =>
        PubSub.Publish(new WindowStateToggledEvent());

    public void Close() =>
        MainWindow.Instance.Dispatcher.Invoke(MainWindow.Instance.Close);

    public void CopyAddress() =>
        PubSub.Publish(new AddressCopyRequestedEvent());

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

        return new DomainTrustRating(rating.Source, rating.Score, rating.Stars, rating.FetchedAt);
    }
}
