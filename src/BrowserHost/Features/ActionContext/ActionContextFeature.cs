using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.ActionContext;

public class ActionContextFeature(PubSub pubSub, IBrowserContext context, ActionContextWindowOperations windowOperations) : Feature(pubSub)
{
    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.S && context.CurrentKeyboardModifiers == ModifierKeys.Control)
        {
            windowOperations.ToggleActionContextVisibility();
            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
