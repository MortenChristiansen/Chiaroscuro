using CefSharp;
using CefSharp.Wpf;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BrowserHost.CefInfrastructure;

public interface IBaseBrowser
{
    void RegisterUiLoaded();
}

public abstract class BaseBrowser : BaseBrowser<BrowserApi>
{
    public override BrowserApi Api { get; }

    protected BaseBrowser()
        : base(null)
    {
        Api = new BrowserApi(this);
    }
}

public abstract class BaseBrowser<TApi> : ChromiumWebBrowser, IBaseBrowser where TApi : BrowserApi
{
    private bool _isUiLoaded = false;
    private readonly string? _uiAddress;

    public abstract TApi Api { get; }

    protected BaseBrowser(string? uiAddress)
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
        SizeChanged += (sender, e) =>
        {
            // Force WebContent to repaint on size change to fix rendering issue
            this.GetBrowserHost()?.Invalidate(PaintElementType.View);
        };
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
        };

        base.BeginInit();
    }

    public void RegisterUiLoaded()
    {
        _isUiLoaded = true;
    }

    protected void RunWhenSourceHasLoaded(Action action)
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
