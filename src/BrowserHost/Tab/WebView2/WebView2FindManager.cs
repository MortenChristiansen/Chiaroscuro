using BrowserHost.Features.TabPalette.FindText;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Threading.Tasks;

namespace BrowserHost.Tab.WebView2;

internal sealed class WebView2FindManager
{
    private CoreWebView2? _core;
    private string? _findTerm;
    private bool _matchCase;
    private int _findIndex;
    private int _findCount;

    public void Initialize(CoreWebView2 core)
    {
        _core = core;
        StopFinding(true);
    }

    public void Find(string searchText, bool forward, bool matchCase, bool findNext)
    {
        if (_core == null) return;
        if (string.IsNullOrWhiteSpace(searchText)) { StopFinding(true); return; }

        // New search if term or matchCase changed, or caller indicates not findNext
        if (_findTerm != searchText || _matchCase != matchCase || !findNext)
        {
            _findTerm = searchText;
            _matchCase = matchCase;
            _findIndex = 0;
            _ = RunHighlightAsync(searchText, matchCase);
            return;
        }

        if (_findCount == 0) return;

        _findIndex = (_findIndex + (forward ? 1 : -1) + _findCount) % _findCount;
        _ = NavigateToCurrentAsync();
    }

    private async Task RunHighlightAsync(string term, bool matchCase)
    {
        if (_core == null) return;
        var js = """
                (function() {
                  const term = TERM_PLACEHOLDER;
                  const matchCase = MATCHCASE_PLACEHOLDER;
                  const CLASS = '__ch_find_highlight';
                  // Remove old
                  const old = document.querySelectorAll('span.'+CLASS);
                  old.forEach(span => {
                    const parent = span.parentNode;
                    while (span.firstChild) parent.insertBefore(span.firstChild, span);
                    parent.removeChild(span);
                    parent && parent.normalize();
                  });
                  if (!term) return 0;
                  const esc = term.replace(/[.*+?^${}()|[\]\\]/g, r => '\\' + r);
                  const re = new RegExp(esc, matchCase ? 'g' : 'gi');
                  const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
                  let count = 0;
                  const textNodes = [];
                  while (walker.nextNode()) textNodes.push(walker.currentNode);

                  const skipTags = new Set(['SCRIPT','STYLE','NOSCRIPT','TEMPLATE','HEAD','META','TITLE']);
                  function isVisible(node) {
                    let el = node.parentElement;
                    if (!el) return false;
                    while (el) {
                      if (skipTags.has(el.tagName)) return false;
                      if (el.hasAttribute('hidden')) return false;
                      if (el.getAttribute && el.getAttribute('aria-hidden') === 'true') return false;
                      const cs = window.getComputedStyle(el);
                      if (cs.display === 'none' || cs.visibility === 'hidden' || cs.opacity === '0') return false;
                      if (el === document.body) return true;
                      el = el.parentElement;
                    }
                    return false;
                  }

                  for (const n of textNodes) {
                    if (!isVisible(n)) continue;
                    const txt = n.nodeValue;
                    if (!txt || !re.test(txt)) { re.lastIndex = 0; continue; }
                    re.lastIndex = 0;
                    let m; let last = 0; let frag = null;
                    while ((m = re.exec(txt)) !== null) {
                      if (!frag) frag = document.createDocumentFragment();
                      const before = txt.slice(last, m.index); if (before) frag.appendChild(document.createTextNode(before));
                      const span = document.createElement('span');
                      span.className = CLASS;
                      span.style.backgroundColor = 'rgba(255,255,0,0.6)';
                      span.style.color = 'black';
                      span.textContent = m[0];
                      frag.appendChild(span);
                      last = m.index + m[0].length;
                      count++;
                    }
                    if (frag) {
                      const after = txt.slice(last); if (after) frag.appendChild(document.createTextNode(after));
                      n.parentNode.replaceChild(frag, n);
                    }
                  }
                  return count;
                })();
                """;
        js = js.Replace("TERM_PLACEHOLDER", JsonSerializer.Serialize(term, BrowserHostJsonContext.Default.String))
               .Replace("MATCHCASE_PLACEHOLDER", matchCase ? "true" : "false");
        try
        {
            var result = await _core.ExecuteScriptAsync(js);
            if (int.TryParse(result, out var count))
            {
                _findCount = count;
                PubSub.Instance.Publish(new FindStatusChangedEvent(count));
                if (count > 0)
                {
                    _findIndex = 0;
                    await NavigateToCurrentAsync();
                }
            }
        }
        catch { }
    }

    private async Task NavigateToCurrentAsync()
    {
        if (_core == null || _findCount == 0) return;
        var js = """
                (function(){
                  const CLASS='__ch_find_highlight';
                  const ACTIVE='__ch_find_active';
                  const spans=Array.from(document.querySelectorAll('span.'+CLASS));
                  spans.forEach((s,i)=>{
                    if (i === FIND_INDEX_PLACEHOLDER) {
                      s.classList.add(ACTIVE);
                      s.style.outline='2px solid orange';
                      s.scrollIntoView({block:'center'});
                    } else {
                      s.classList.remove(ACTIVE);
                      s.style.outline='';
                    }
                  });
                  return spans.length;
                })();
                """;
        js = js.Replace("FIND_INDEX_PLACEHOLDER", _findIndex.ToString());
        try
        {
            var result = await _core.ExecuteScriptAsync(js);
            if (int.TryParse(result, out var count) && count != _findCount)
            {
                _findCount = count;
                PubSub.Instance.Publish(new FindStatusChangedEvent(count));
            }
        }
        catch { }
    }

    public void StopFinding(bool clearSelection)
    {
        _findTerm = null;
        _findIndex = 0;
        _findCount = 0;
        if (_core == null) return;
        var js = """
                (function(){
                  const CLASS='__ch_find_highlight';
                  const spans=document.querySelectorAll('span.'+CLASS);
                  spans.forEach(span=>{
                    const parent=span.parentNode;
                    while(span.firstChild) parent.insertBefore(span.firstChild, span);
                    parent.removeChild(span);
                    parent && parent.normalize();
                  });
                  CLEAR_SEL_PLACEHOLDER
                })();
                """;
        if (clearSelection)
            js = js.Replace("CLEAR_SEL_PLACEHOLDER", "try { window.getSelection().removeAllRanges(); } catch {}");
        else
            js = js.Replace("CLEAR_SEL_PLACEHOLDER", string.Empty);
        _ = _core.ExecuteScriptAsync(js);
        PubSub.Instance.Publish(new FindStatusChangedEvent(0));
    }
}
