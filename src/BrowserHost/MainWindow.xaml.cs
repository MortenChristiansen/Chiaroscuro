using CefSharp;
using CefSharp.Wpf;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Velopack;
using Velopack.Sources;

namespace BrowserHost;

public partial class MainWindow : Window
{
    private BrowserApi _browserApi;

    public MainWindow()
    {
        InitializeComponent();

        CheckForUpdates();

        _browserApi = new BrowserApi(this);

        // Not sure if this does anything
        WebContent.BrowserSettings.WindowlessFrameRate = 120;
        WebContent.BrowserSettings.WebGl = CefState.Enabled;
        ChromeUI.BrowserSettings.WindowlessFrameRate = 120;
        ChromeUI.BrowserSettings.WebGl = CefState.Enabled;
        ActionDialog.BrowserSettings.WindowlessFrameRate = 120;
        ActionDialog.BrowserSettings.WebGl = CefState.Enabled;

        ChromeUI.Address = ContentServer.GetUiAddress("/");
        ChromeUI.JavascriptObjectRepository.Register("api", _browserApi);
        ChromeUI.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"ChromeUI: {e.Message}");
        };

        ActionDialog.Address = ContentServer.GetUiAddress("/action-dialog");
        ActionDialog.ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"ActionDialog: {e.Message}");
        };
        ActionDialog.JavascriptObjectRepository.Register("api", _browserApi);

        CurrentTab.AddressChanged += CurrentTab_AddressChanged;

        ContentServer.Run();
    }

    private static async void CheckForUpdates()
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource("https://github.com/MortenChristiansen/Chiaroscuro", accessToken: null, prerelease: false, downloader: null));
            var updateInfo = await mgr.CheckForUpdatesAsync();
            if (updateInfo != null)
            {
                var result = MessageBox.Show(
                    "A new version is available. Would you like to update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    await mgr.DownloadUpdatesAsync(updateInfo);
                    mgr.ApplyUpdatesAndRestart(updateInfo);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        // Improves issue with rendering stopping on resize
        RootGrid.Children.OfType<ChromiumWebBrowser>()
            .ToList()
            .ForEach(browser => browser.GetBrowserHost()?.Invalidate(PaintElementType.View));
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.T && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            _browserApi.ShowActionDialog();
            e.Handled = true;
        }

        if (e.Key == Key.F5)
        {
            var ignoreCache = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            WebContent.Reload(ignoreCache);
        }
    }

    private void CurrentTab_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _browserApi.ChangeAddress($"{e.NewValue}");
    }

    public ChromiumWebBrowser Chrome => ChromeUI;
    public ChromiumWebBrowser CurrentTab => WebContent;
}
