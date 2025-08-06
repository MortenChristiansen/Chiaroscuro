using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette;

public record TabPaletteRequestedEvent();
public record TabPaletteDismissedEvent();

public class TabPaletteFeature(MainWindow window) : Feature(window)
{
    private bool _tabPaletteIsOpen;

    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => OpenTabPalette());
        PubSub.Subscribe<TabPaletteDismissedEvent>((_) => CloseTabPalette());
        PubSub.Subscribe<TabDeactivatedEvent>((_) => CloseTabPalette());
        PubSub.Subscribe<FindTextEvent>((e) => Window.CurrentTab?.GetBrowser().Find(e.Term, true, false, findNext: true));
        PubSub.Subscribe<NextTextMatchEvent>((e) => Window.CurrentTab?.GetBrowser().Find(e.Term, forward: true, false, findNext: true));
        PubSub.Subscribe<PrevTextMatchEvent>((e) => Window.CurrentTab?.GetBrowser().Find(e.Term, forward: false, false, findNext: true));
        PubSub.Subscribe<StopFindingTextEvent>((_) => Window.CurrentTab?.GetBrowser().StopFinding(true));
        PubSub.Subscribe<FindStatusChangedEvent>((e) => Window.TabPaletteBrowserControl.FindStatusChanged(e.Matches));
    }

    public void OpenTabPalette()
    {
        _tabPaletteIsOpen = true;
        Window.TabPaletteBrowserControl.Init();
        Window.ShowTabPalette();
    }

    private void CloseTabPalette()
    {
        _tabPaletteIsOpen = false;
        Window.HideTabPalette();
        Window.CurrentTab?.GetBrowser().StopFinding(true);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F1)
        {
            if (_tabPaletteIsOpen)
            {
                PubSub.Publish(new TabPaletteDismissedEvent());

            }
            else
            {
                PubSub.Publish(new TabPaletteRequestedEvent());
            }
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
