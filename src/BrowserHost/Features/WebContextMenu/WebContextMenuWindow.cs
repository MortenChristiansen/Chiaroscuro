using BrowserHost.XamlUtilities;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuWindow : OverlayWindow
{
    private readonly Border _container;
    private readonly WebContextMenuBrowser _browser = new()
    {
        // Ensure a non-zero initial size so CefSharp can initialize
        MinWidth = 1,
        MinHeight = 1,
    };

    public WebContextMenuWindow(Window owner, double x, double y)
    {
        Owner = owner;
        // Ensure OverlayWindow can track and size relative to the owner window
        OwnerWindow = owner;
        Left = x;
        Top = y;

        _browser.BeginInit();
        _browser.EndInit();

        // Build a visible shell for the menu so something is shown regardless of browser content
        _container = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0.01
        };

        // Ensure the browser fills the container
        _browser.HorizontalAlignment = HorizontalAlignment.Stretch;
        _browser.VerticalAlignment = VerticalAlignment.Stretch;

        _container.Child = _browser;
        Content = _container;
    }

    private Timer? _timer;
    private bool _allowCloseAfterAnimation;

    public void Prepare(ContextMenuParameters parameters)
    {
        _browser.SetParameters(parameters);

        _timer = new Timer(_ =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (!_browser.IsBrowserInitialized)
                    return;

                _browser.GetContentSizeAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted || t.IsCanceled)
                        return;

                    var size = t.Result;
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (_browser.Width == size.Width && _browser.Height == size.Height)
                        {
                            _timer?.Dispose();
                            _timer = null;
                            PlayShowAnimation();
                        }

                        // 2 pixels prevent scrollbars from appearing for some reason
                        _browser.Width = size.Width + 2;
                        _browser.Height = size.Height + 2;
                    });
                });
            });
        }, null, 0, 50);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowCloseAfterAnimation)
        {
            e.Cancel = true;
            PlayHideAnimation();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _timer?.Dispose();
        _timer = null;
        try { _browser.Dispose(); } catch { /* best-effort */ }
    }

    private void PlayShowAnimation()
    {
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        _container.BeginAnimation(UIElement.OpacityProperty, fadeIn);
    }

    private void PlayHideAnimation()
    {
        var fadeOut = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        fadeOut.Completed += (_, __) =>
        {
            _allowCloseAfterAnimation = true;
            Close();
        };
        _container.BeginAnimation(UIElement.OpacityProperty, fadeOut);
    }
}
