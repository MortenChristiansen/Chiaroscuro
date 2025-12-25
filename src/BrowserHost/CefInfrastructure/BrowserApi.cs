using CefSharp;

namespace BrowserHost.CefInfrastructure;

public class BrowserApi(BaseBrowser browser)
{
    public virtual void CallClientApi(string api, string? arguments = null)
    {
        var modifiedScript =
            $$"""
               function tryRun_{{api}}() {
                 if (window.angularApi && window.angularApi.{{api}}) {
                    window.angularApi.{{api}}.call({{arguments}});
                 } else {
                   setTimeout(tryRun_{{api}}, 50);
                 }
               }
               tryRun_{{api}}();
               """;

        browser.Dispatcher.BeginInvoke(() =>
        {
            if (browser.IsBrowserInitialized)
            {
                browser.ExecuteScriptAsync(modifiedScript);
            }
            else
            {
                browser.IsBrowserInitializedChanged += (sender, e) =>
                {
                    if (!browser.IsDisposed)
                        ExecuteScriptOnDispatcher(modifiedScript);
                };
            }
        });
    }

    private void ExecuteScriptOnDispatcher(string script)
    {
        browser.Dispatcher.BeginInvoke(() =>
        {
            browser.ExecuteScriptAsync(script);
        });
    }
}
