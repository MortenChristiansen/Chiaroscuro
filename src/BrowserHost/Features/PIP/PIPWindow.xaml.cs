using BrowserHost.Features.Tabs;
using CefSharp;
using CefSharp.Wpf;
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace BrowserHost.Features.PIP;

public partial class PIPWindow : Window
{
    private readonly TabBrowser _videoTab;
    private readonly PIPFeature _pipFeature;
    private DispatcherTimer? _hideControlsTimer;
    private bool _isVideoPlaying = true;
    private ChromiumWebBrowser? _pipBrowser;

    public PIPWindow(TabBrowser videoTab, PIPFeature pipFeature)
    {
        InitializeComponent();

        _videoTab = videoTab;
        _pipFeature = pipFeature;

        // Position window at bottom-right of screen
        var workingArea = SystemParameters.WorkArea;
        Left = workingArea.Right - Width - 20;
        Top = workingArea.Bottom - Height - 20;

        _hideControlsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _hideControlsTimer.Tick += (s, e) =>
        {
            HideControls();
            _hideControlsTimer.Stop();
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetupPIPContent();
        HideControls();
    }

    private void SetupPIPContent()
    {
        try
        {
            // Create a new browser instance to display the same page
            _pipBrowser = new ChromiumWebBrowser(_videoTab.Address)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Add the browser to the video border
            VideoBorder.Child = _pipBrowser;

            // Wait for the browser to load and then inject script to show only video
            _pipBrowser.LoadingStateChanged += (sender, args) =>
            {
                if (!args.IsLoading)
                {
                    // Inject script to hide everything except the video
                    Dispatcher.BeginInvoke(() => InjectVideoOnlyScript());
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up PIP content: {ex.Message}");
            // Fallback to placeholder
            SetupPlaceholderContent();
        }
    }

    private void SetupPlaceholderContent()
    {
        // Set a dark background to indicate video area
        VideoBorder.Background = System.Windows.Media.Brushes.Black;

        // Add a text overlay to show this is a PIP preview
        var textBlock = new System.Windows.Controls.TextBlock
        {
            Text = "📹 Video Playing",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };

        var grid = new System.Windows.Controls.Grid();
        grid.Children.Add(textBlock);
        VideoBorder.Child = grid;
    }

    private void InjectVideoOnlyScript()
    {
        if (_pipBrowser?.IsBrowserInitialized == true)
        {
            var script = @"
                (function() {
                    // Find the first video element
                    const video = document.querySelector('video');
                    if (video) {
                        // Hide everything except the video
                        document.body.style.margin = '0';
                        document.body.style.padding = '0';
                        document.body.style.overflow = 'hidden';
                        document.body.style.background = 'black';
                        
                        // Hide all elements except video
                        const allElements = document.querySelectorAll('*:not(video):not(source)');
                        allElements.forEach(el => {
                            if (el !== video && !video.contains(el) && el !== document.body && el !== document.documentElement) {
                                el.style.display = 'none';
                            }
                        });
                        
                        // Style the video to fill the container
                        video.style.position = 'fixed';
                        video.style.top = '0';
                        video.style.left = '0';
                        video.style.width = '100vw';
                        video.style.height = '100vh';
                        video.style.objectFit = 'contain';
                        video.style.background = 'black';
                        video.style.zIndex = '9999';
                        
                        // Remove controls to avoid interference
                        video.controls = false;
                        
                        // Sync with original video
                        const syncWithOriginal = () => {
                            try {
                                // This would ideally sync with the original tab's video
                                // For now, we just ensure the video is playing
                                if (video.paused) {
                                    video.play().catch(() => {});
                                }
                            } catch (e) {
                                console.log('Sync error:', e);
                            }
                        };
                        
                        // Try to sync every second
                        setInterval(syncWithOriginal, 1000);
                    }
                })();
            ";

            _pipBrowser.ExecuteScriptAsync(script);
        }
    }

    private async void SyncVideoState(object? sender, EventArgs e)
    {
        if (_videoTab?.IsBrowserInitialized == true && !_videoTab.IsDisposed)
        {
            try
            {
                var script = @"
                    (function() {
                        const videos = document.querySelectorAll('video');
                        if (videos.length > 0) {
                            const video = videos[0];
                            return {
                                playing: !video.paused && !video.ended,
                                currentTime: video.currentTime,
                                duration: video.duration,
                                title: document.title
                            };
                        }
                        return null;
                    })();
                ";

                var result = await _videoTab.EvaluateScriptAsync(script);
                if (result.Success && result.Result != null)
                {
                    // For now, just update the play/pause button state
                    // In a full implementation, you would extract the video data
                    var videoData = result.Result.ToString();
                    if (videoData?.Contains("\"playing\":true") == true)
                    {
                        _isVideoPlaying = true;
                        Dispatcher.Invoke(() => PlayPauseIcon.Text = "⏸");
                    }
                    else if (videoData?.Contains("\"playing\":false") == true)
                    {
                        _isVideoPlaying = false;
                        Dispatcher.Invoke(() => PlayPauseIcon.Text = "▶");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing video state: {ex.Message}");
            }
        }
    }

    private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        ShowControls();
        _hideControlsTimer?.Stop();
    }

    private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _hideControlsTimer?.Start();
    }

    private void ShowControls()
    {
        var storyboard = new Storyboard();
        var animation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
        Storyboard.SetTarget(animation, ControlOverlay);
        Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty));
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void HideControls()
    {
        var storyboard = Resources["FadeOutStoryboard"] as Storyboard;
        storyboard?.Begin();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _pipFeature.ClosePIP();
    }

    private void ActivateTabButton_Click(object sender, RoutedEventArgs e)
    {
        _pipFeature.ActivateVideoTab();
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        _pipFeature.ToggleVideoPlayback();

        // Toggle local state for immediate UI feedback
        _isVideoPlaying = !_isVideoPlaying;
        PlayPauseIcon.Text = _isVideoPlaying ? "⏸" : "▶";
    }

    protected override void OnClosed(EventArgs e)
    {
        _hideControlsTimer?.Stop();
        _pipBrowser?.Dispose();
        base.OnClosed(e);
    }
}