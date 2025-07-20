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
    private ChromiumWebBrowser? _pipBrowser;
    private DispatcherTimer? _hideControlsTimer;
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
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SetupPIPBrowser();
        HideControls();
    }

    private void SetupPIPBrowser()
    {
        // Create a new browser instance that mirrors the video tab
        _pipBrowser = new ChromiumWebBrowser(_videoTab.Address)
        {
            BrowserSettings = new BrowserSettings
            {
                BackgroundColor = Cef.ColorSetARGB(255, 0, 0, 0)
            }
        };

        // Inject CSS to hide everything except video elements and make them fullscreen
        _pipBrowser.FrameLoadEnd += (sender, args) =>
        {
            if (args.Frame.IsMain)
            {
                var script = @"
                    (function() {
                        // Hide all elements except videos
                        const style = document.createElement('style');
                        style.textContent = `
                            * { display: none !important; }
                            video { 
                                display: block !important; 
                                position: fixed !important;
                                top: 0 !important;
                                left: 0 !important;
                                width: 100vw !important;
                                height: 100vh !important;
                                object-fit: contain !important;
                                z-index: 999999 !important;
                                background: black !important;
                            }
                            body, html {
                                margin: 0 !important;
                                padding: 0 !important;
                                overflow: hidden !important;
                                background: black !important;
                                display: block !important;
                            }
                        `;
                        document.head.appendChild(style);
                        
                        // Find and configure video elements
                        const videos = document.querySelectorAll('video');
                        videos.forEach(video => {
                            video.controls = false;
                            video.style.display = 'block';
                            video.style.position = 'fixed';
                            video.style.top = '0';
                            video.style.left = '0';
                            video.style.width = '100vw';
                            video.style.height = '100vh';
                            video.style.objectFit = 'contain';
                            video.style.zIndex = '999999';
                            video.style.background = 'black';
                        });
                        
                        // Sync playback state with original video
                        const originalVideos = window.parent ? 
                            window.parent.document.querySelectorAll('video') : [];
                        
                        if (originalVideos.length > 0 && videos.length > 0) {
                            const originalVideo = originalVideos[0];
                            const pipVideo = videos[0];
                            
                            // Sync time
                            pipVideo.currentTime = originalVideo.currentTime;
                            
                            // Sync play state
                            if (originalVideo.paused) {
                                pipVideo.pause();
                            } else {
                                pipVideo.play().catch(() => {});
                            }
                        }
                    })();
                ";
                
                args.Frame.ExecuteJavaScriptAsync(script);
                
                // Update play/pause button state
                CheckVideoPlayState();
            }
        };

        VideoBorder.Child = _pipBrowser;
    }

    private async void CheckVideoPlayState()
    {
        if (_pipBrowser?.IsBrowserInitialized == true)
        {
            try
            {
                var script = @"
                    (function() {
                        const videos = document.querySelectorAll('video');
                        return videos.length > 0 ? !videos[0].paused : false;
                    })();
                ";
                
                var result = await _pipBrowser.EvaluateScriptAsync(script);
                if (result.Success && result.Result is bool isPlaying)
                {
                    _isVideoPlaying = isPlaying;
                    Dispatcher.Invoke(() =>
                    {
                        PlayPauseIcon.Text = _isVideoPlaying ? "⏸" : "▶";
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking video play state: {ex.Message}");
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
        
        // Also control local PIP video
        if (_pipBrowser?.IsBrowserInitialized == true)
        {
            var script = @"
                (function() {
                    const videos = document.querySelectorAll('video');
                    videos.forEach(video => {
                        if (video.paused) {
                            video.play().catch(() => {});
                        } else {
                            video.pause();
                        }
                    });
                })();
            ";
            _pipBrowser.ExecuteScriptAsync(script);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _hideControlsTimer?.Stop();
        _pipBrowser?.Dispose();
        base.OnClosed(e);
    }
}