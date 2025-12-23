using System.Windows.Input;

namespace BrowserHost.Features.Zoom;

public class ZoomFeature(MainWindow window, IBrowserContext browserContext) : Feature(window)
{
    protected virtual ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public override bool HandleOnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (CurrentKeyboardModifiers == ModifierKeys.Control && browserContext.CurrentTab != null)
        {
            var currentZoomLevel = browserContext.CurrentTab.GetZoomLevelAsync().GetAwaiter().GetResult();

            if (e.Delta > 0 && currentZoomLevel < 10)
                currentZoomLevel += 0.2;
            else if (e.Delta < 0 && currentZoomLevel > -10)
                currentZoomLevel -= 0.2;


            browserContext.CurrentTab.SetZoomLevel(currentZoomLevel);
            return true;
        }

        return base.HandleOnPreviewMouseWheel(e);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete && CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            if (browserContext.CurrentTab != null)
                browserContext.CurrentTab.ResetZoomLevel();

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
