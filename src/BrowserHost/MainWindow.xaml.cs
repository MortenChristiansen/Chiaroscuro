using BrowserHost.Features;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DevTool;
using BrowserHost.Features.Tabs;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Velopack;
using Velopack.Sources;

namespace BrowserHost;

public partial class MainWindow : Window
{
    private readonly List<Feature> _features;

    public ChromiumWebBrowser Chrome => ChromeUI;
    public TabBrowser? CurrentTab => (TabBrowser)WebContentBorder.Child;

    public static MainWindow Instance { get; private set; } = null!; // Initialized in constructor

    public MainWindow()
    {
        InitializeComponent();

        CheckForUpdates();

        _features =
        [
            new CustomWindowChromeFeature(this),
            new ActionDialogFeature(this),
            new TabsFeature(this),
            new DevToolFeature(this)
        ];
        _features.ForEach(f => f.Register());

        ContentServer.Run();
        Instance = this;
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
                    Application.Current.MainWindow,
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

        foreach (var feature in _features)
        {
            if (feature.HandleOnPreviewKeyDown(e))
            {
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.F5)
        {
            var ignoreCache = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            CurrentTab.Reload(ignoreCache);
        }
    }

    public void SetCurrentTab(TabBrowser? tab)
    {
        if (CurrentTab != null)
            CurrentTab.AddressChanged -= Tab_AddressChanged;

        WebContentBorder.Child = tab;
        ChromeUI.ChangeAddress(tab?.Address);

        if (tab != null)
            tab.AddressChanged += Tab_AddressChanged;
    }

    private void Tab_AddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        ChromeUI.ChangeAddress($"{e.NewValue}");
    }
}
