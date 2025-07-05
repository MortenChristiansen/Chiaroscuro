using CefSharp;
using CefSharp.Wpf;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BrowserHost.CefInfrastructure;

public interface IBaseBrowser
{
}

public abstract class BaseBrowser : ChromiumWebBrowser
{
    // This base class tries to remedy the issue of CefSharp not redrawing the browser when the size changes.
    // https://github.com/cefsharp/CefSharp/issues/4953

    private static readonly ConcurrentDictionary<BaseBrowser, byte> _browsersScheduledForRedraw = [];

    static BaseBrowser()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                var browsers = _browsersScheduledForRedraw.ToArray();
                _browsersScheduledForRedraw.Clear();

                foreach (var browser in browsers)
                    browser.Key.Redraw();

                await Task.Delay(25);

                foreach (var browser in browsers)
                    browser.Key.Redraw();

                await Task.Delay(25);

                foreach (var browser in browsers)
                    browser.Key.Redraw();

                await Task.Delay(25);

                foreach (var browser in browsers)
                    browser.Key.Redraw();

                await Task.Delay(25);

                foreach (var browser in browsers)
                    browser.Key.Redraw();
            }
        });
    }

    protected BaseBrowser()
    {
        SizeChanged += (sender, e) => _browsersScheduledForRedraw.AddOrUpdate(this, 0, (_, _) => 0);
    }

    private void Redraw()
    {
        this.GetBrowserHost()?.Invalidate(PaintElementType.View);
    }
}

public abstract class Browser(string? uiAddress = null) : Browser<BrowserApi>(uiAddress)
{
    public override BrowserApi Api { get; } = new BrowserApi();

    protected void RegisterSecondaryApi<TApi>(TApi api, string name) where TApi : BrowserApi
    {
        JavascriptObjectRepository.Register(name, api);
    }
}

public abstract class Browser<TApi> : BaseBrowser, IBaseBrowser where TApi : BrowserApi
{
    private readonly string? _uiAddress;

    public abstract TApi Api { get; }

    protected Browser(string? uiAddress)
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

        _uiAddress = uiAddress;
    }

    public override void BeginInit()
    {
        JavascriptObjectRepository.Register("api", Api);
        if (_uiAddress != null)
            Address = ContentServer.GetUiAddress(_uiAddress);

        ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"{GetType().Name}: {e.Message}");
            if (e.Level == LogSeverity.Error && Debugger.IsAttached)
                this.GetBrowserHost().ShowDevTools();
        };

        base.BeginInit();
    }

    public void CallClientApi(string api, string? arguments = null)
    {
        var modifiedScript =
            $$"""
               function tryRun_{{api}}() {
                 if (window.angularApi && window.angularApi.{{api}}) {
                    window.angularApi.{{api}}({{arguments}});
                 } else {
                   setTimeout(tryRun_{{api}}, 50);
                 }
               }
               tryRun_{{api}}();
               """;

        Dispatcher.BeginInvoke(() =>
        {
            if (IsBrowserInitialized)
            {
                this.ExecuteScriptAsync(modifiedScript);
            }
            else
            {
                IsBrowserInitializedChanged += (sender, e) =>
                {
                    if (!IsDisposed)
                        ExecuteScriptOnDispatcher(modifiedScript);
                };
            }
        });
    }

    private void ExecuteScriptOnDispatcher(string script)
    {
        Dispatcher.BeginInvoke(() =>
        {
            this.ExecuteScriptAsync(script);
        });
    }
}

public class BrowserApi()
{
}
