using BrowserHost.XamlUtilities;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuWindow : OverlayWindow
{
    //private readonly Border _menuContainer;
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
        // TODO: Fixes dismiss logic activating other applications, though we need it to be activated eventually
        ShowActivated = false; // Do not activate/focus this overlay window
        Focusable = false; // Prevent keyboard focus

        _browser.BeginInit();
        _browser.EndInit();

        // Build a visible shell for the menu so something is shown regardless of browser content
        var container = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 1 // TODO: Animate
        };

        // Ensure the browser fills the container
        _browser.HorizontalAlignment = HorizontalAlignment.Stretch;
        _browser.VerticalAlignment = VerticalAlignment.Stretch;

        container.Child = _browser;
        Content = container;
    }

    private Timer? _timer;

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
                    var size = t.Result;
                    Dispatcher.BeginInvoke(() =>
                    {
                        if (_browser.Width == size.Width && _browser.Height == size.Height)
                        {
                            _timer?.Dispose();
                            _timer = null;
                        }

                        // 2 pixels prevent scrollbars from appearing for some reason
                        _browser.Width = size.Width + 2;
                        _browser.Height = size.Height + 2;
                    });
                });
            });
        }, null, 0, 50);
    }
}
