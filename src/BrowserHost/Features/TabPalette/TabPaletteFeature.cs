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
    private string? _findingTextTerm;

    public override void Configure()
    {
        PubSub.Subscribe<TabPaletteRequestedEvent>((_) => OpenTabPalette());
        PubSub.Subscribe<TabPaletteDismissedEvent>((_) => CloseTabPalette());
        PubSub.Subscribe<TabDeactivatedEvent>((_) => CloseTabPalette());
        PubSub.Subscribe<FindTextEvent>((e) => StartFinding(e.Term));
        PubSub.Subscribe<NextTextMatchEvent>((e) => FindNext(e.Term));
        PubSub.Subscribe<PrevTextMatchEvent>((e) => FindPrevious(e.Term));
        PubSub.Subscribe<StopFindingTextEvent>((_) => StopFinding());
        PubSub.Subscribe<FindStatusChangedEvent>((e) => Window.TabPaletteBrowserControl.FindStatusChanged(e.Matches));
    }

    private void StartFinding(string term)
    {
        Window.CurrentTab?.GetBrowser().Find(term, true, false, findNext: true);
        _findingTextTerm = term;
    }

    private void FindNext(string term)
    {
        Window.CurrentTab?.GetBrowser().Find(term, forward: true, false, findNext: true);
    }

    private void FindPrevious(string term)
    {
        Window.CurrentTab?.GetBrowser().Find(term, forward: false, false, findNext: true);
    }

    private void StopFinding()
    {
        Window.CurrentTab?.GetBrowser().StopFinding(true);
        Window.TabPaletteBrowserControl.FindStatusChanged(null);
        _findingTextTerm = null;
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
        PubSub.Publish(new StopFindingTextEvent());
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (_findingTextTerm != null && e.Key == Key.Tab)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
                PubSub.Publish(new PrevTextMatchEvent(_findingTextTerm));
            else
                PubSub.Publish(new NextTextMatchEvent(_findingTextTerm));

            return true;
        }

        if (_findingTextTerm != null && e.Key == Key.Escape)
        {
            PubSub.Publish(new StopFindingTextEvent());

            return true;
        }

        if (e.Key == Key.F1)
        {
            if (_tabPaletteIsOpen)
                PubSub.Publish(new TabPaletteDismissedEvent());
            else
                PubSub.Publish(new TabPaletteRequestedEvent());

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }
}
