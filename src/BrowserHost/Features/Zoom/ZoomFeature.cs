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
            var currentZoomLevel = Window.CurrentTab?.GetZoomLevelAsync().GetAwaiter().GetResult() ?? 0;

            if (e.Delta > 0 && currentZoomLevel < 10)
                currentZoomLevel += 0.2;
            else if (e.Delta < 0 && currentZoomLevel > -10)
                currentZoomLevel -= 0.2;

            if (Window.CurrentTab is not null)
            {
                Window.CurrentTab.SetZoomLevel(currentZoomLevel);
                return true;
            }
        }

        return base.HandleOnPreviewMouseWheel(e);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Back && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (Window.CurrentTab is not null)
            {
                Window.CurrentTab.SetZoomLevel(0);
                return true;
            }

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
