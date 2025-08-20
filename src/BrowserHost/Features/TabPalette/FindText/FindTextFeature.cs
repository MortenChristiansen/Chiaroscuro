using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette.FindText;

public class FindTextFeature(MainWindow window) : Feature(window)
{
    private string? _findingTextTerm;

    public override void Configure()
    {
        PubSub.Subscribe<FindTextEvent>((e) => StartFinding(e.Term));
        PubSub.Subscribe<NextTextMatchEvent>((e) => FindNext(e.Term));
        PubSub.Subscribe<PrevTextMatchEvent>((e) => FindPrevious(e.Term));
        PubSub.Subscribe<StopFindingTextEvent>((_) => StopFinding());
        PubSub.Subscribe<FindStatusChangedEvent>((e) => Window.TabPaletteBrowserControl.FindStatusChanged(e.Matches));

        PubSub.Subscribe<TabPaletteDismissedEvent>((_) => PubSub.Publish(new StopFindingTextEvent()));
        PubSub.Subscribe<TabDeactivatedEvent>((_) => PubSub.Publish(new StopFindingTextEvent()));
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

        return base.HandleOnPreviewKeyDown(e);
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
}
