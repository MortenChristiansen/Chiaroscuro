using BrowserHost.Utilities;
using CefSharp;
using CefSharp.Structs;

namespace BrowserHost.Features.TabPalette.FindText;

public class FindHandler(PubSub pubSub) : CefSharp.Handler.FindHandler
{
    protected override void OnFindResult(IWebBrowser chromiumWebBrowser, IBrowser browser, int identifier, int count, Rect selectionRect, int activeMatchOrdinal, bool finalUpdate)
    {
        base.OnFindResult(chromiumWebBrowser, browser, identifier, count, selectionRect, activeMatchOrdinal, finalUpdate);
        pubSub.Send(new ChangeFindStatusCommand(count));
    }
}
