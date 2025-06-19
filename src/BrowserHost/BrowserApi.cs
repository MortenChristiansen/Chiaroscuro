using CefSharp;
using System.Windows;

namespace BrowserHost;

internal class BrowserApi(MainWindow window)
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

    public void ShowActionDialog()
    {
        if (window.ActionDialog.Visibility == Visibility.Visible)
            return;

        window.ActionDialog.Visibility = Visibility.Visible;
        window.ActionDialog.Focus();
        window.ActionDialog.ExecuteScriptAsync("window.angularApi.showDialog()");
    }

    public void DismissActionDialog()
    {
        if (window.ActionDialog.Visibility == Visibility.Hidden)
            return;

        window.Dispatcher.Invoke(() =>
        {
            window.ActionDialog.Visibility = Visibility.Hidden;
        });
    }

    public void Minimize()
    {
        window.Dispatcher.Invoke(() =>
        {
            if (window.WindowState == WindowState.Minimized)
                return;

            window.WindowState = WindowState.Minimized;
        });
    }

    public void Maximize()
    {
        window.Dispatcher.Invoke(() =>
        {
            if (window.WindowState == WindowState.Maximized)
                return;

            window.WindowState = WindowState.Maximized;
        });
    }

    public void Close()
    {
        window.Dispatcher.Invoke(window.Close);
    }
}
