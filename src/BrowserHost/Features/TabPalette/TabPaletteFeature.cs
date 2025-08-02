using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette;

public record TabPaletteRequestedEvent();
public record TabPaletteDismissedEvent();

public class TabPaletteFeature(MainWindow window) : Feature(window)
{
    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) =>
        {
            Window.ShowTabPalette();
        });
        PubSub.Subscribe<TabPaletteDismissedEvent>((_) =>
        {
            Window.HideTabPalette();
        });
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        // TODO: Do we want any shortcuts for this specifically?
        if (e.Key == Key.F1)
        {
            PubSub.Publish(new TabPaletteRequestedEvent());
        }
        if (e.Key == Key.F2)
        {
            PubSub.Publish(new TabPaletteDismissedEvent());
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
