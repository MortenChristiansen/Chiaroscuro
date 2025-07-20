using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace BrowserHost.Features.PIP;

public class PIPFeature(MainWindow window) : Feature(window)
{
    private PIPWindow? _pipWindow;
    private TabBrowser? _currentVideoTab;
    private bool _isVideoPlaying = false;
    private Timer? _videoPollingTimer;

    public override void Register()
    {
        PubSub.Subscribe<TabActivatedEvent>(OnTabActivated);
        PubSub.Subscribe<TabClosedEvent>(OnTabClosed);
        PubSub.Subscribe<TabLoadingStateChangedEvent>(OnTabLoadingStateChanged);
        
        // Set up periodic video checking
        _videoPollingTimer = new Timer(2000); // Check every 2 seconds
        _videoPollingTimer.Elapsed += CheckAllTabsForVideo;
        _videoPollingTimer.Start();
    }

    private void OnTabActivated(TabActivatedEvent e)
    {
        // If switching back to the video tab, hide PIP
        if (e.CurrentTab == _currentVideoTab && _pipWindow != null)
        {
            HidePIP();
            return;
        }

        // If switching away from a video tab, show PIP if video is playing
        if (_currentVideoTab != null && _isVideoPlaying && e.CurrentTab != _currentVideoTab)
        {
            ShowPIP(_currentVideoTab);
        }
    }

    private void OnTabClosed(TabClosedEvent e)
    {
        if (e.Tab == _currentVideoTab)
        {
            HidePIP();
            _currentVideoTab = null;
            _isVideoPlaying = false;
        }
    }

    private void OnTabLoadingStateChanged(TabLoadingStateChangedEvent e)
    {
        // Check for video a bit after page loads
        if (!e.IsLoading)
        {
            _ = Task.Delay(3000).ContinueWith(_ => CheckSpecificTabForVideo(e.TabId));
        }
    }

    private void CheckAllTabsForVideo(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var tabsFeature = Window.GetFeature<TabsFeature>();
            Window.Dispatcher.Invoke(() =>
            {
                var currentTab = Window.CurrentTab;
                if (currentTab != null)
                {
                    CheckSpecificTabForVideo(currentTab.Id);
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in video polling: {ex.Message}");
        }
    }

    private void CheckSpecificTabForVideo(string tabId)
    {
        try
        {
            var tabsFeature = Window.GetFeature<TabsFeature>();
            var tab = tabsFeature.GetTabById(tabId);
            if (tab == null || tab.IsDisposed) return;

            var script = @"
                (function() {
                    const videos = document.querySelectorAll('video');
                    for (let video of videos) {
                        if (!video.paused && !video.ended && video.readyState >= 2) {
                            return true;
                        }
                    }
                    return false;
                })();
            ";

            tab.Dispatcher.Invoke(async () =>
            {
                if (!tab.IsBrowserInitialized) return;

                try
                {
                    var result = await tab.EvaluateScriptAsync(script);
                    if (result.Success && result.Result is bool isPlaying)
                    {
                        var wasPlaying = _isVideoPlaying && _currentVideoTab == tab;
                        
                        if (isPlaying && (!wasPlaying || _currentVideoTab != tab))
                        {
                            // Video started playing or different tab started playing
                            _currentVideoTab = tab;
                            _isVideoPlaying = true;
                            
                            // Show PIP if this is not the current tab
                            if (Window.CurrentTab != tab)
                            {
                                ShowPIP(tab);
                            }
                        }
                        else if (!isPlaying && wasPlaying)
                        {
                            // Video stopped playing
                            _isVideoPlaying = false;
                            if (_currentVideoTab == tab)
                            {
                                HidePIP();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error checking for video in tab {tabId}: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in CheckSpecificTabForVideo: {ex.Message}");
        }
    }

    private void ShowPIP(TabBrowser videoTab)
    {
        if (_pipWindow != null) return;

        Window.Dispatcher.Invoke(() =>
        {
            try
            {
                _pipWindow = new PIPWindow(videoTab, this);
                _pipWindow.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing PIP window: {ex.Message}");
            }
        });
    }

    private void HidePIP()
    {
        if (_pipWindow == null) return;

        Window.Dispatcher.Invoke(() =>
        {
            try
            {
                _pipWindow?.Close();
                _pipWindow = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding PIP window: {ex.Message}");
            }
        });
    }

    public void ActivateVideoTab()
    {
        if (_currentVideoTab != null)
        {
            Window.ActionContext.ActivateTab(_currentVideoTab.Id);
            HidePIP();
        }
    }

    public void ToggleVideoPlayback()
    {
        if (_currentVideoTab == null) return;

        var script = @"
            (function() {
                const videos = document.querySelectorAll('video');
                videos.forEach(video => {
                    if (!video.paused && !video.ended) {
                        video.pause();
                    } else {
                        video.play().catch(() => {});
                    }
                });
            })();
        ";

        _currentVideoTab.Dispatcher.Invoke(() =>
        {
            if (_currentVideoTab.IsBrowserInitialized)
            {
                _currentVideoTab.ExecuteScriptAsync(script);
            }
        });
    }

    public void ClosePIP()
    {
        HidePIP();
    }

    public void Cleanup()
    {
        _videoPollingTimer?.Stop();
        _videoPollingTimer?.Dispose();
        HidePIP();
    }
}