using CefSharp;
using CefSharp.Wpf;
using System.Diagnostics;
using System.Linq;

namespace BrowserHost;

internal class BrowserApi
{
    private static readonly string _actionDialogUrl = ContentServer.GetUiAddress("/action-dialog");
    private readonly MainWindow _window;

    public BrowserApi(MainWindow window)
    {
        _window = window;
        _window.CurrentTab.AddressChanged += CurrentTab_AddressChanged;
        _window.ChromeUI.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"Console message from ChromeUI: {e.Message} (line {e.Line})");
        };
    }

    private void CurrentTab_AddressChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        _window.ChromeUI.ExecuteScriptAsync($"window.angularApi.changeAddress('{e.NewValue}')");
    }

    public bool CanGoForward()
    {
        return _window.Dispatcher.Invoke(() => _window.CurrentTab.CanGoForward);
    }

    public void Forward()
    {
        _window.CurrentTab.Forward();
    }

    public bool CanGoBack()
    {
        return _window.Dispatcher.Invoke(() => _window.CurrentTab.CanGoBack);
    }

    public void Back()
    {
        _window.CurrentTab.Back();
    }

    public void Navigate(string url)
    {
        Debug.WriteLine($"Visiting url: {url}");
        _window.CurrentTab.LoadUrl(url);
    }

    public void ShowActionDialog()
    {
        Debug.WriteLine($"Showing action dialog");
        var dialogBrowser = new ChromiumWebBrowser
        {
            Address = _actionDialogUrl,
        };
        dialogBrowser.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"Console message from ActionDialog: {e.Message} (line {e.Line})");
        };
        dialogBrowser.JavascriptObjectRepository.Register("api", this);
        _window.RootGrid.Children.Add(dialogBrowser);
        dialogBrowser.Focus();
    }

    public void DismissActionDialog()
    {
        Debug.WriteLine($"Hiding action dialog");
        _window.Dispatcher.Invoke(() =>
        {
            var dialog = _window.RootGrid.Children.OfType<ChromiumWebBrowser>().FirstOrDefault(browser => browser.Address == _actionDialogUrl);

            if (dialog != null)
            {
                _window.RootGrid.Children.Remove(dialog);
                dialog.Dispose();
            }
        });
    }
}
