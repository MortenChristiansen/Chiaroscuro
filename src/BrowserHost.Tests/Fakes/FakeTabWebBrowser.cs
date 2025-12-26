using BrowserHost.CefInfrastructure;
using BrowserHost.Tab;
using System.Windows;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabWebBrowser(string tabId) : ITabWebBrowser
{
    public string Id { get; } = tabId;
    public string? Favicon => null;
    public string? ManualAddress => null;
    public string Address => "about:blank";
    public string Title { get; set; } = "";
    public bool IsLoading => false;
    public bool CanGoBack => false;
    public bool CanGoForward => false;
    public bool HasDevTools => false;
    public double DefaultZoomLevel => 0;

    public event DependencyPropertyChangedEventHandler? AddressChanged;
    public event EventHandler? PageLoadEnded;

    public bool SupportsPromotionToFullTab => throw new NotImplementedException();
    public void SetAddress(string address, bool setManualAddress) => throw new NotSupportedException();
    public void RegisterContentPageApi(BackendApi api, string name) => throw new NotSupportedException();
    public void Reload(bool ignoreCache = false) => throw new NotSupportedException();
    public void Back() => throw new NotSupportedException();
    public void Forward() => throw new NotSupportedException();
    public Task CallClientApi(string api, string? arguments = null) => throw new NotSupportedException();
    public Task ExecuteScriptAsync(string script) => throw new NotSupportedException();
    public Task<double> GetZoomLevelAsync() => throw new NotSupportedException();
    public void SetZoomLevel(double level) => throw new NotSupportedException();
    public void Find(string searchText, bool forward, bool matchCase, bool findNext) => throw new NotSupportedException();
    public void StopFinding(bool clearSelection) => throw new NotSupportedException();
    public UIElement AsUIElement() => throw new NotSupportedException();
    public void ShowDevTools() => throw new NotSupportedException();
    public void CloseDevTools() => throw new NotSupportedException();
    public void Dispose() { }
}
