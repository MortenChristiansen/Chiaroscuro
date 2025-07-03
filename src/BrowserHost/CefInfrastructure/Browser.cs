using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BrowserHost.CefInfrastructure;

public interface IBaseBrowser
{
    void RegisterUiLoaded();
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

public abstract class Browser : Browser<BrowserApi>
{
    public override BrowserApi Api { get; }

    protected Browser(string? uiAddress = null)
        : base(uiAddress)
    {
        Api = new BrowserApi(this);
    }

    protected void RegisterSecondaryApi<TApi>(TApi api, string name) where TApi : BrowserApi
    {
        JavascriptObjectRepository.Register(name, api);
    }
}

public abstract class Browser<TApi> : BaseBrowser, IBaseBrowser where TApi : BrowserApi
{
    private bool _isUiLoaded = false;
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
            if (e.Message.Contains("ERROR") && Debugger.IsAttached)
                this.GetBrowserHost().ShowDevTools();
        };

        base.BeginInit();
    }

    public void RegisterUiLoaded()
    {
        _isUiLoaded = true;
    }

    public void RunWhenSourceHasLoaded(Action action)
    {
        if (_isUiLoaded)
        {
            action();
        }
        else
        {
            Task.Run(async () =>
            {
                while (!_isUiLoaded)
                {
                    await Task.Delay(100); // Wait until the source is loaded
                }
                Dispatcher.Invoke(action);
            });
        }
    }
}

public class BrowserApi(IBaseBrowser browser)
{
    public void UiLoaded()
    {
        browser.RegisterUiLoaded();
    }
}
