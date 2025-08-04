using BrowserHost.Utilities;
using CefSharp;
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
        PubSub.Subscribe<FindTextEvent>((e) =>
        {
            if (Window.CurrentTab == null) return;
            Window.CurrentTab.GetBrowser().Find(e.Term, true, false, findNext: true);
        });
        PubSub.Subscribe<NextTextMatchEvent>((e) =>
        {
            if (Window.CurrentTab == null) return;
            Window.CurrentTab.GetBrowser().Find(e.Term, forward: true, false, findNext: true);
        });
        PubSub.Subscribe<PrevTextMatchEvent>((e) =>
        {
            if (Window.CurrentTab == null) return;
            Window.CurrentTab.GetBrowser().Find(e.Term, forward: false, false, findNext: true);
        });
        PubSub.Subscribe<StopFindingTextEvent>((_) =>
        {
            if (Window.CurrentTab == null) return;
            Window.CurrentTab.GetBrowser().StopFinding(true);
        });
        PubSub.Subscribe<FindStatusChangedEvent>((e) =>
        {
            Window.TabPaletteBrowserControl.FindStatusChanged(e.Matches);
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
