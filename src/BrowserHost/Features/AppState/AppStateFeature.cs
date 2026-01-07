using BrowserHost.Utilities;

namespace BrowserHost.Features.AppState;

public class AppStateFeature(PubSub pubSub, IBrowserContext context, AppStateStateManager stateManager) : Feature(pubSub)
{
    public override void Configure()
    {
        context.ActionContextResizeCompleted += () =>
        {
            var width = context.ActionContextActualWidth;
            stateManager.SaveActionContextWidth(width);
        };

        context.TabPaletteResizeCompleted += () =>
        {
            var width = context.TabPaletteActualWidth;
            if (width > 0)
                stateManager.SaveTabPaletteWidth(width);
        };
    }

    public override void Start()
    {
        ApplyInitialLayout();
    }

    private void ApplyInitialLayout()
    {
        var layout = stateManager.RestoreAppStateFromDisk();

        if (layout.ActionContextWidth > 0)
            context.SetActionContextWidth(layout.ActionContextWidth);

        // TabPalette is restored when opened; keep collapsed until user shows it
    }
}
