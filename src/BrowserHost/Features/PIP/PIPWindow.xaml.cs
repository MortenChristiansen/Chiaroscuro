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
    private DispatcherTimer? _videoSyncTimer;
    private bool _isVideoPlaying = true;

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

        // Sync timer to check video state
        _videoSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _videoSyncTimer.Tick += SyncVideoState;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetupPIPContent();
        HideControls();
        _videoSyncTimer?.Start();
    }

    private void SetupPIPContent()
    {
        // Instead of creating a new browser, we'll inject a small preview into the original tab
        // and capture that. For now, we'll show a placeholder and rely on the controls
        
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
        _videoSyncTimer?.Stop();
        base.OnClosed(e);
    }
}