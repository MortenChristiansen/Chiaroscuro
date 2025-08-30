using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using CefSharp;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserHost.Tab.CefSharp;

public class CefSharpTabBrowserAdapter(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon) : ITabWebBrowser
{
    private readonly CefSharpTabBrowser _cefBrowser = new(id, address, actionContextBrowser, setManualAddress, favicon);

    public string Id => _cefBrowser.Id;
    public string? Favicon => _cefBrowser.Favicon;
    public string? ManualAddress => _cefBrowser.ManualAddress;
    public string Address => _cefBrowser.Address;
    public string Title
    {
        get => _cefBrowser.Title;
        set => _cefBrowser.Title = value;
    }
    public bool IsLoading => _cefBrowser.IsLoading;
    public bool CanGoBack => _cefBrowser.CanGoBack;
    public bool CanGoForward => _cefBrowser.CanGoForward;
    public double DefaultZoomLevel => 0.0;

    public bool HasDevTools => _cefBrowser.GetBrowserHost().HasDevTools;

    public event DependencyPropertyChangedEventHandler? AddressChanged
    {
        add => _cefBrowser.AddressChanged += value;
        remove => _cefBrowser.AddressChanged -= value;
    }

    public void SetAddress(string address, bool setManualAddress) => _cefBrowser.SetAddress(address, setManualAddress);
    public void RegisterContentPageApi(BrowserApi api, string name) => _cefBrowser.RegisterContentPageApi(api, name);
    public void Reload(bool ignoreCache = false) => _cefBrowser.Reload(ignoreCache);
    public void Dispose() => _cefBrowser.Dispose();
    public void Back() => _cefBrowser.Back();
    public void Forward() => _cefBrowser.Forward();
    public Task CallClientApi(string api, string? arguments = null) { _cefBrowser.CallClientApi(api, arguments); return Task.CompletedTask; }
    public object? GetBrowserHost() => _cefBrowser.GetBrowserHost();
    public Task<double> GetZoomLevelAsync() => _cefBrowser.GetZoomLevelAsync();
    public void SetZoomLevel(double level) => _cefBrowser.SetZoomLevel(level);
    public void Find(string searchText, bool forward, bool matchCase, bool findNext) => _cefBrowser.Find(searchText, forward, matchCase, findNext);
    public void StopFinding(bool clearSelection) => _cefBrowser.StopFinding(clearSelection);
    public UIElement AsUIElement() => _cefBrowser;
    public void ShowDevTools() => _cefBrowser.ShowDevTools();
    public void CloseDevTools() => _cefBrowser.CloseDevTools();
}
