using BrowserHost.Features.ActionContext;
using BrowserHost.Features.TabPalette;
using BrowserHost.Utilities;

namespace BrowserHost.Features.AppState;

public class AppStateFeature(PubSub pubSub, ActionContextWindowOperations actionContextWindowOperations, TabPaletteWindowOperations tabPaletteWindowOperations, AppStateStateManager stateManager) : Feature(pubSub)
{
    public override void Configure()
    {
        actionContextWindowOperations.RegisterActionContextResizeCompletedHandler(() =>
        {
            var width = actionContextWindowOperations.ActionContextActualWidth;
            stateManager.SaveActionContextWidth(width);
        });

        tabPaletteWindowOperations.RegisterTabPaletteResizeCompletedHandler(() =>
        {
            var width = tabPaletteWindowOperations.TabPaletteActualWidth;
            if (width > 0)
                stateManager.SaveTabPaletteWidth(width);
        });
    }

    public override void Start()
    {
        ApplyInitialLayout();
    }

    private void ApplyInitialLayout()
    {
        var layout = stateManager.RestoreAppStateFromDisk();

        if (layout.ActionContextWidth > 0)
            actionContextWindowOperations.SetActionContextWidth(layout.ActionContextWidth);

        // TabPalette is restored when opened; keep collapsed until user shows it
    }
}
