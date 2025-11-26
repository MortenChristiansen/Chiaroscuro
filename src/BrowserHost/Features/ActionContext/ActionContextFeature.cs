using System;
using System.Windows.Input;

namespace BrowserHost.Features.ActionContext;

public class ActionContextFeature(MainWindow window) : Feature(window)
{
    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ToggleActionContextHidden();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void ToggleActionContextHidden()
    {
        throw new NotImplementedException();
    }
}
