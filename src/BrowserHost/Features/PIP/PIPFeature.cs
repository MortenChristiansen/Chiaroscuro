using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Threading.Tasks;

namespace BrowserHost.Features.PIP;

public class PIPFeature(MainWindow window) : Feature(window)
{
    private PIPWindow? _pipWindow;
    private TabBrowser? _currentVideoTab;
    private bool _isVideoPlaying = false;
    private TabBrowser? _previousTab;

    public override void Register()
    {
        PubSub.Subscribe<TabActivatedEvent>(OnTabActivated);
        PubSub.Subscribe<TabClosedEvent>(OnTabClosed);
        PubSub.Subscribe<TabLoadingStateChangedEvent>(OnTabLoadingStateChanged);
    }

    private void OnTabActivated(TabActivatedEvent e)
    {
        // If switching back to the video tab, hide PIP
        if (e.CurrentTab == _currentVideoTab && _pipWindow != null)
        {
            HidePIP();
        }

        // If we have a previous tab that was just deactivated, check if it has playing video
        if (_previousTab != null && _previousTab != e.CurrentTab)
        {
            CheckSpecificTabForVideo(_previousTab.Id, true);
        }

        // Update previous tab for next time
        _previousTab = e.CurrentTab;
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
            _ = Task.Delay(3000).ContinueWith(_ => CheckSpecificTabForVideo(e.TabId, false));
        }
    }

    private void CheckSpecificTabForVideo(string tabId, bool isTabBecomingInactive)
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

                            // Show PIP if this tab is becoming inactive (user switched away)
                            if (isTabBecomingInactive)
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
        HidePIP();
    }
}