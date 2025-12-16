using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using CefSharp;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserHost.Tab.CefSharp;

public class CefSharpTabBrowserAdapter : ITabWebBrowser
{
    private readonly CefSharpTabBrowser _cefBrowser;

    public CefSharpTabBrowserAdapter(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        _cefBrowser = new(id, address, actionContextBrowser, setManualAddress, favicon, isChildBrowser);
        _cefBrowser.LoadingStateChanged += OnPageLoadEnded;
    }

    private void OnPageLoadEnded(object? sender, LoadingStateChangedEventArgs e)
    {
        if (!e.IsLoading)
            _cefBrowser.Dispatcher.InvokeAsync(() => PageLoadEnded?.Invoke(this, EventArgs.Empty));
    }

    public string Id => _cefBrowser.Id;
    public string? Favicon => RunOnUi(() => _cefBrowser.Favicon);
    public string? ManualAddress => _cefBrowser.ManualAddress;
    public string Address => RunOnUi(() => _cefBrowser.Address);
    public string Title
    {
        get => RunOnUi(() => _cefBrowser.Title);
        set => RunOnUi(() => _cefBrowser.Title = value);
    }
    public bool IsLoading => RunOnUi(() => _cefBrowser.IsLoading);
    public bool CanGoBack => RunOnUi(() => _cefBrowser.CanGoBack);
    public bool CanGoForward => RunOnUi(() => _cefBrowser.CanGoForward);
    public double DefaultZoomLevel => 0.0;

    public bool HasDevTools => RunOnUi(() => _cefBrowser.GetBrowserHost().HasDevTools);

    public bool SupportsPromotionToFullTab => true;

    public event EventHandler? PageLoadEnded;

    public event DependencyPropertyChangedEventHandler? AddressChanged
    {
        add => _cefBrowser.AddressChanged += value;
        remove => _cefBrowser.AddressChanged -= value;
    }

    private T RunOnUi<T>(Func<T> action)
    {
        if (_cefBrowser.Dispatcher.CheckAccess()) return action();
        else return _cefBrowser.Dispatcher.Invoke(action);
    }

    public void SetAddress(string address, bool setManualAddress) => _cefBrowser.SetAddress(address, setManualAddress);
    public void RegisterContentPageApi(BrowserApi api, string name) => _cefBrowser.RegisterContentPageApi(api, name);
    public void Reload(bool ignoreCache = false) => _cefBrowser.Reload(ignoreCache);
    public void Dispose()
    {
        _cefBrowser.LoadingStateChanged -= OnPageLoadEnded;
        _cefBrowser.Dispose();
    }
    public void Back() => _cefBrowser.Back();
    public void Forward() => _cefBrowser.Forward();
    public Task CallClientApi(string api, string? arguments = null) { _cefBrowser.CallClientApi(api, arguments); return Task.CompletedTask; }
    public Task ExecuteScriptAsync(string script)
    {
        if (_cefBrowser.IsDisposed)
            return Task.CompletedTask;

        if (_cefBrowser.IsBrowserInitialized)
        {
            _cefBrowser.ExecuteScriptAsync(script);
        }
        else
        {
            void handler(object s, DependencyPropertyChangedEventArgs e)
            {
                _cefBrowser.IsBrowserInitializedChanged -= handler;
                if (_cefBrowser.IsDisposed)
                    return;

                _cefBrowser.ExecuteScriptAsync(script);
            }

            _cefBrowser.IsBrowserInitializedChanged += handler;

        }
        return Task.CompletedTask;
    }
    public object? GetBrowserHost() => _cefBrowser.GetBrowserHost();
    public Task<double> GetZoomLevelAsync() => _cefBrowser.GetZoomLevelAsync();
    public void SetZoomLevel(double level) => _cefBrowser.SetZoomLevel(level);
    public void Find(string searchText, bool forward, bool matchCase, bool findNext) => _cefBrowser.Find(searchText, forward, matchCase, findNext);
    public void StopFinding(bool clearSelection) => _cefBrowser.StopFinding(clearSelection);
    public UIElement AsUIElement() => _cefBrowser;
    public void ShowDevTools() => _cefBrowser.ShowDevTools();
    public void CloseDevTools()
    {
        try
        {
            _cefBrowser.CloseDevTools();
        }
        catch
        {
            // Might fail if the tab is disposed
        }
    }
}
