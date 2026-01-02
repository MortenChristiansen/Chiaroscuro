using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Utilities;
using System.Windows.Input;

namespace BrowserHost.Features.TabPalette.FindText;

public class FindTextFeature(MainWindow window, PubSub pubSub, IBrowserContext browserContext, FindTextBrowserApi findTextApi) : Feature(window, pubSub)
{
    private string? _findingTextTerm;

    public override void Configure()
    {
        PubSub.Handle<FindTextCommand>(cmd =>
        {
            StartFinding(cmd.Term);
            PubSub.Publish(new FindTextTriggeredEvent(cmd.Term));
        });
        PubSub.Handle<FindNextTextMatchCommand>(cmd =>
        {
            FindNext(cmd.Term);
            PubSub.Publish(new NextTextMatchTriggeredEvent(cmd.Term));
        });
        PubSub.Handle<FindPrevTextMatchCommand>(cmd =>
        {
            FindPrevious(cmd.Term);
            PubSub.Publish(new PrevTextMatchTriggeredEvent(cmd.Term));
        });
        PubSub.Handle<StopFindingTextCommand>((_) =>
        {
            StopFinding();
            PubSub.Publish(new StopFindingTextTriggeredEvent());
        });
        PubSub.Handle<ChangeFindStatusCommand>(cmd =>
        {
            findTextApi.FindStatusChanged(cmd.Matches);
            PubSub.Publish(new FindStatusChangedEvent(cmd.Matches));
        });

        PubSub.Subscribe<TabPaletteDismissedEvent>((_) => PubSub.Send(new StopFindingTextCommand()));
        PubSub.Subscribe<TabDeactivatedEvent>((_) => PubSub.Send(new StopFindingTextCommand()));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        if (_findingTextTerm != null && e.Key == Key.Tab)
        {
            if (browserContext.CurrentKeyboardModifiers == ModifierKeys.Shift)
                PubSub.Send(new FindPrevTextMatchCommand(_findingTextTerm));
            else
                PubSub.Send(new FindNextTextMatchCommand(_findingTextTerm));

            return true;
        }

        if (_findingTextTerm != null && e.Key == Key.Escape)
        {
            PubSub.Send(new StopFindingTextCommand());

            return true;
        }

        if (_findingTextTerm == null && (e.Key == Key.F3 || (e.Key == Key.F && browserContext.CurrentKeyboardModifiers == ModifierKeys.Control)))
        {
            PubSub.Send(new RequestTabPaletteCommand());
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
