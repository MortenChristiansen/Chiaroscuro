using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette.FindText;

public class FindTextFeature(MainWindow window, IBrowserContext browserContext, FindTextBrowserApi findTextApi) : Feature(window)
{
    private string? _findingTextTerm;

    public override void Configure()
    {
        PubSub.Instance.Subscribe<FindTextEvent>((e) => StartFinding(e.Term));
        PubSub.Instance.Subscribe<NextTextMatchEvent>((e) => FindNext(e.Term));
        PubSub.Instance.Subscribe<PrevTextMatchEvent>((e) => FindPrevious(e.Term));
        PubSub.Instance.Subscribe<StopFindingTextEvent>((_) => StopFinding());
        PubSub.Instance.Subscribe<FindStatusChangedEvent>((e) => findTextApi.FindStatusChanged(e.Matches));

        PubSub.Instance.Subscribe<TabPaletteDismissedEvent>((_) => PubSub.Instance.Publish(new StopFindingTextEvent()));
        PubSub.Instance.Subscribe<TabDeactivatedEvent>((_) => PubSub.Instance.Publish(new StopFindingTextEvent()));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (_findingTextTerm != null && e.Key == Key.Tab)
        {
            if (browserContext.CurrentKeyboardModifiers == ModifierKeys.Shift)
                PubSub.Instance.Publish(new PrevTextMatchEvent(_findingTextTerm));
            else
                PubSub.Instance.Publish(new NextTextMatchEvent(_findingTextTerm));

            return true;
        }

        if (_findingTextTerm != null && e.Key == Key.Escape)
        {
            PubSub.Instance.Publish(new StopFindingTextEvent());

            return true;
        }

        if (_findingTextTerm == null && (e.Key == Key.F3 || (e.Key == Key.F && browserContext.CurrentKeyboardModifiers == ModifierKeys.Control)))
        {
            PubSub.Instance.Publish(new TabPaletteRequestedEvent());
            browserContext.FocusTabPalette();
            findTextApi.FocusFindTextInput();

            return true;
        }

        return base.HandleOnPreviewKeyDown(e);
    }

    private void StartFinding(string term)
    {
        browserContext.CurrentTab?.Find(term, forward: true, matchCase: false, findNext: true);
        _findingTextTerm = term;
    }

    private void FindNext(string term)
    {
        browserContext.CurrentTab?.Find(term, forward: true, matchCase: false, findNext: true);
    }

    private void FindPrevious(string term)
    {
        browserContext.CurrentTab?.Find(term, forward: false, matchCase: false, findNext: true);
    }

    private void StopFinding()
    {
        browserContext.CurrentTab?.StopFinding(true);
        findTextApi.FindStatusChanged(null);
        _findingTextTerm = null;
    }
}
