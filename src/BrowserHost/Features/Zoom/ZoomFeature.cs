using System.Threading.Tasks;
using System.Windows.Input;

namespace BrowserHost.Features.Zoom;

public class ZoomFeature(MainWindow window) : Feature(window)
{
    protected virtual ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    protected virtual Task<double?> GetCurrentZoomLevelAsync() =>
        Window.CurrentTab is null ? Task.FromResult<double?>(null) : Window.CurrentTab.GetZoomLevelAsync().ContinueWith(t => (double?)t.Result);

    protected virtual void SetCurrentZoomLevel(double level) => Window.CurrentTab?.SetZoomLevel(level);
    protected virtual void ResetCurrentZoomLevel() => Window.CurrentTab?.ResetZoomLevel();
    protected virtual bool HasCurrentTab => Window.CurrentTab is not null;

    public override bool HandleOnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        if (CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            var currentZoomLevel = GetCurrentZoomLevelAsync().GetAwaiter().GetResult() ?? 0;

            if (e.Delta > 0 && currentZoomLevel < 10)
                currentZoomLevel += 0.2;
            else if (e.Delta < 0 && currentZoomLevel > -10)
                currentZoomLevel -= 0.2;

            if (HasCurrentTab)
            {
                SetCurrentZoomLevel(currentZoomLevel);
                return true;
            }
        }

        return base.HandleOnPreviewMouseWheel(e);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete && CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            if (HasCurrentTab)
            {
                ResetCurrentZoomLevel();
                return true;
            }

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
