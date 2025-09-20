using BrowserHost.Logging;
using CefSharp;
using CefSharp.Wpf;
using System.Diagnostics;
using System.Windows.Media;

namespace BrowserHost.CefInfrastructure;

public interface IBaseBrowser
{
}

public abstract class BaseBrowser : ChromiumWebBrowser
{
    public override void BeginInit()
    {
        AllowDrop = false;

        base.BeginInit();
    }
}

public abstract class Browser(string? uiAddress = null, bool disableContextMenu = false) : Browser<BrowserApi>(uiAddress, disableContextMenu)
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
    private readonly bool _disableContextMenu;

    public abstract TApi Api { get; }

    protected Browser(string? uiAddress, bool disableContextMenu)
    {
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);

        _uiAddress = uiAddress;
        _disableContextMenu = disableContextMenu;
    }

    public override void BeginInit()
    {
        JavascriptObjectRepository.Register("api", Api);
        if (_uiAddress != null)
            Address = ContentServer.GetUiAddress(_uiAddress);

        ConsoleMessage += (sender, e) =>
        {
            Debug.WriteLine($"{GetType().Name}: {e.Message}");
            
            // Log console errors to the application log (only for non-TabBrowser browsers)
            if (e.Level == LogSeverity.Error)
            {
                LoggingService.Instance.Log(LogType.ConsoleErrors, $"{GetType().Name}: {e.Message}");
                
                if (Debugger.IsAttached)
                    this.GetBrowserHost().ShowDevTools();
            }
        };

        if (_disableContextMenu)
            MenuHandler = new DisabledContextMenuHandler();

        base.BeginInit();
    }

    public void CallClientApi(string api, string? arguments = null)
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
