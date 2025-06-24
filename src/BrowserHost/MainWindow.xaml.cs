using BrowserHost.Features;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Velopack;
using Velopack.Sources;

namespace BrowserHost;

public partial class MainWindow : Window
{
    public CustomWindowChromeFeature CustomWindowChromeFeature { get; }
    public ActionDialogFeature ActionDialogFeature { get; }

    public ChromiumWebBrowser Chrome => ChromeUI;
    public ChromiumWebBrowser CurrentTab => WebContent;

    public MainWindow()
    {
        InitializeComponent();

        CheckForUpdates();

        var api = new BrowserApi(this);

        CustomWindowChromeFeature = new(this, api);
        CustomWindowChromeFeature.Register();
        ActionDialogFeature = new(this, api);
        ActionDialogFeature.Register();

        CurrentTab.AddressChanged += CurrentTab_AddressChanged;

        ContentServer.Run();
    }

    private static async void CheckForUpdates()
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource("https://github.com/MortenChristiansen/Chiaroscuro", accessToken: null, prerelease: false, downloader: null));
            if (mgr.CurrentVersion is null)
                return;

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

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (ActionDialogFeature.HandleOnPreviewKeyDown(e))
            return;

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
}
