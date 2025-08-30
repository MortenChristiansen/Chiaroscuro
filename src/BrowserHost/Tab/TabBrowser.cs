using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Tab.CefSharp;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BrowserHost.Tab;

public class TabBrowser : UserControl
{
    private readonly ITabWebBrowser _browser;

    public string Id => _browser.Id;
    public string? Favicon => _browser.Favicon;
    public string? ManualAddress => _browser.ManualAddress;
    public string Address => _browser.Address;
    public string Title
    {
        get => _browser.Title;
        set => _browser.Title = value;
    }
    public bool IsLoading => _browser.IsLoading;
    public bool CanGoBack => _browser.CanGoBack;
    public bool CanGoForward => _browser.CanGoForward;
    public bool HasDevTools => _browser.HasDevTools;

    public event DependencyPropertyChangedEventHandler? AddressChanged
    {
        add => _browser.AddressChanged += value;
        remove => _browser.AddressChanged -= value;
    }

    public TabBrowser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon)
    {
        _browser = new CefSharpTabBrowserAdapter(id, address, actionContextBrowser, setManualAddress, favicon);
        Content = _browser.AsUIElement();
    }

    public void SetAddress(string address, bool setManualAddress) => _browser.SetAddress(address, setManualAddress);
    public void RegisterContentPageApi(BrowserApi api, string name) => _browser.RegisterContentPageApi(api, name);
    public void Reload(bool ignoreCache = false) => _browser.Reload(ignoreCache);
    public void Dispose() => _browser.Dispose();
    public void Back() => _browser.Back();
    public void Forward() => _browser.Forward();
    public void CallClientApi(string api, string? arguments = null) => _browser.CallClientApi(api, arguments);
    public Task<double> GetZoomLevelAsync() => _browser.GetZoomLevelAsync();
    public void ResetZoomLevel() => _browser.SetZoomLevel(_browser.DefaultZoomLevel);
    public void SetZoomLevel(double level) => _browser.SetZoomLevel(level);
    public void Find(string searchText, bool forward, bool matchCase, bool findNext) => _browser.Find(searchText, forward, matchCase, findNext);
    public void StopFinding(bool clearSelection) => _browser.StopFinding(clearSelection);
    public void ShowDevTools() => _browser.ShowDevTools();
    public void CloseDevTools() => _browser.CloseDevTools();
}
