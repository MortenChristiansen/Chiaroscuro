using CefSharp;
using System.Windows.Input;

namespace BrowserHost.Features.Zoom;

public class ZoomFeature(MainWindow window) : Feature(window)
{
    public override void Register()
    {
    }

    public override bool HandleOnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            var _currentZoomLevel = Window.CurrentTab?.GetZoomLevelAsync().GetAwaiter().GetResult() ?? 0;

            if (e.Delta > 0 && _currentZoomLevel < 10)
                _currentZoomLevel += 0.2;
            else if (e.Delta < 0 && _currentZoomLevel > -10)
                _currentZoomLevel -= 0.2;

            if (Window.CurrentTab is IChromiumWebBrowserBase browser)
            {
                browser.SetZoomLevel(_currentZoomLevel);
                return true;
            }
        }

        return base.HandleOnPreviewMouseWheel(e);
    }
}
