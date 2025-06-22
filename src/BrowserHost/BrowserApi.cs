using CefSharp;

namespace BrowserHost;

public class BrowserApi(MainWindow window)
{
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
        window.CurrentTab.LoadUrl(url);

    public void Reload() =>
        window.WebContent.Reload();

    public void DismissActionDialog() =>
        window.Dispatcher.Invoke(window.ActionDialogFeature.DismissDialog);

    public void Minimize() =>
        window.Dispatcher.Invoke(window.CustomWindowChromeFeature.Minimize);

    public void Maximize() =>
        window.Dispatcher.Invoke(window.CustomWindowChromeFeature.ToggleMaximizedState);

    public void Close() =>
        window.Dispatcher.Invoke(window.Close);
}
