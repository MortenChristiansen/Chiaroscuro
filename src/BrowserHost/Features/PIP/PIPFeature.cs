using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Diagnostics;

namespace BrowserHost.Features.PIP;

public class PIPFeature(MainWindow window) : Feature(window)
{
    private PIPWindow? _pipWindow;
    private TabBrowser? _currentVideoTab;

    public override void Register()
    {
        PubSub.Subscribe<TabActivatedEvent>(OnTabActivated);
        PubSub.Subscribe<TabClosedEvent>(OnTabClosed);
    }

    private void OnTabActivated(TabActivatedEvent e)
    {
        // If switching back to the video tab, hide PIP
        if (e.TabId == _currentVideoTab?.Id && _pipWindow != null)
        {
            HidePIP();
            ToggleVideoPlayback();
        }

        // If we have a previous tab that was just deactivated, check if it has playing video
        if (e.PreviousTab != null)
        {
            CheckSpecificTabForVideo(e.PreviousTab.Id, true);
        }
    }

    private void OnTabClosed(TabClosedEvent e)
    {
        if (e.Tab == _currentVideoTab)
        {
            HidePIP();
            _currentVideoTab = null;
        }
    }

    private void CheckSpecificTabForVideo(string tabId, bool isTabBecomingInactive)
    {
        Debug.WriteLine("Checking for video in tab " + tabId);
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
                        if (isPlaying && isTabBecomingInactive)
                        {
                            _currentVideoTab = tab;
                            ToggleVideoPlayback();
                            ShowPIP(tab);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking for video in tab {tabId}: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in CheckSpecificTabForVideo: {ex.Message}");
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
                Debug.WriteLine($"Error showing PIP window: {ex.Message}");
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
                Debug.WriteLine($"Error hiding PIP window: {ex.Message}");
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

        // TODO: It should not toggle, but set a specific state
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