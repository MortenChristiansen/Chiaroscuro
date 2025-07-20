using BrowserHost.Features.Tabs;
using CefSharp;
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
    private PIPBrowser? _pipBrowser;

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

    private async void SetupPIPContent()
    {
        try
        {
            // Get direct video stream URL and timestamp from the original tab
            string videoUrl = string.Empty;
            double timestamp = 0;

            if (_videoTab.IsBrowserInitialized)
            {
                var script = @"
                    (function() {
                        const video = document.querySelector('video');
                        return video ? { src: video.currentSrc || video.src, time: video.currentTime } : null;
                    })();
                ";
                var result = await _videoTab.EvaluateScriptAsync(script);
                if (result.Success && result.Result != null)
                {
                    // Try to extract src and time from result.Result as a dynamic object or dictionary
                    string? src = null;
                    double time = 0;
                    var dict = result.Result as System.Collections.IDictionary;
                    if (dict != null)
                    {
                        src = dict["src"]?.ToString();
                        double.TryParse(dict["time"]?.ToString(), out time);
                    }
                    else
                    {
                        // Fallback: try to use reflection for anonymous type
                        var type = result.Result.GetType();
                        var srcProp = type.GetProperty("src");
                        var timeProp = type.GetProperty("time");
                        if (srcProp != null)
                            src = srcProp.GetValue(result.Result)?.ToString();
                        if (timeProp != null)
                        {
                            var val = timeProp.GetValue(result.Result);
                            if (val != null)
                                double.TryParse(val.ToString(), out time);
                        }
                    }
                    if (!string.IsNullOrEmpty(src))
                        videoUrl = src;
                    timestamp = time;
                }
            }

            if (string.IsNullOrEmpty(videoUrl))
            {
                // Fallback to tab address if no video src found
                videoUrl = _videoTab.Address;
            }

            // Base64 encode the direct video stream URL
            string base64Url = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(videoUrl));

            // Create a new browser instance to display the custom player
            _pipBrowser = new PIPBrowser(base64Url, timestamp)
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            // Add the browser to the video border
            VideoBorder.Child = _pipBrowser;
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

    //private async void SyncVideoState(object? sender, EventArgs e)
    //{
    //    if (_videoTab?.IsBrowserInitialized == true && !_videoTab.IsDisposed)
    //    {
    //        try
    //        {
    //            var script = @"
    //                (function() {
    //                    const videos = document.querySelectorAll('video');
    //                    if (videos.length > 0) {
    //                        const video = videos[0];
    //                        return {
    //                            playing: !video.paused && !video.ended,
    //                            currentTime: video.currentTime,
    //                            duration: video.duration,
    //                            title: document.title
    //                        };
    //                    }
    //                    return null;
    //                })();
    //            ";

    //            var result = await _videoTab.EvaluateScriptAsync(script);
    //            if (result.Success && result.Result != null)
    //            {
    //                // For now, just update the play/pause button state
    //                // In a full implementation, you would extract the video data
    //                var videoData = result.Result.ToString();
    //                if (videoData?.Contains("\"playing\":true") == true)
    //                {
    //                    _isVideoPlaying = true;
    //                    Dispatcher.Invoke(() => PlayPauseIcon.Text = "⏸");
    //                }
    //                else if (videoData?.Contains("\"playing\":false") == true)
    //                {
    //                    _isVideoPlaying = false;
    //                    Dispatcher.Invoke(() => PlayPauseIcon.Text = "▶");
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            System.Diagnostics.Debug.WriteLine($"Error syncing video state: {ex.Message}");
    //        }
    //    }
    //}

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