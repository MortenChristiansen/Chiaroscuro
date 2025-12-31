using BrowserHost.CefInfrastructure;
using BrowserHost.Tab;
using System.Windows;

namespace BrowserHost.Tests.Fakes;

internal class FakeTabWebBrowser(string tabId) : ITabWebBrowser
{
    public string Id { get; } = tabId;
    public string? Favicon => null;
    public string? ManualAddress => null;
    public string Address { get; private set; } = "about:blank";
    public string Title { get; set; } = "";
    public bool IsLoading => false;
    public bool CanGoBack => false;
    public bool CanGoForward => false;
    public bool HasDevTools => false;
    public double DefaultZoomLevel => 0;

    public List<ClientApiInvocation> ClientApiInvocations { get; } = [];
    public List<RegisteredContentPageApi> RegisteredContentPageApis { get; } = [];

    public List<string> ExecutedScripts { get; } = [];

    public event DependencyPropertyChangedEventHandler? AddressChanged;
    public event EventHandler? PageLoadEnded;

    public bool SupportsPromotionToFullTab => throw new NotSupportedException();
    public void SetAddress(string address, bool setManualAddress)
    {
        var old = Address;
        Address = address;
        AddressChanged?.Invoke(this, new DependencyPropertyChangedEventArgs(null!, old, address));
    }
    public void RegisterContentPageApi(BackendApi api, string name) =>
        RegisteredContentPageApis.Add(new RegisteredContentPageApi(name, api));
    public void Reload(bool ignoreCache = false) => throw new NotSupportedException();
    public void Back() => throw new NotSupportedException();
    public void Forward() => throw new NotSupportedException();
    public Task CallClientApi(string api, string? arguments = null)
    {
        ClientApiInvocations.Add(new ClientApiInvocation(api, arguments));
        return Task.CompletedTask;
    }
    public Task ExecuteScriptAsync(string script)
    {
        ExecutedScripts.Add(script);
        return Task.CompletedTask;
    }
    public Task<double> GetZoomLevelAsync() => throw new NotSupportedException();
    public void SetZoomLevel(double level) => throw new NotSupportedException();
    public void Find(string searchText, bool forward, bool matchCase, bool findNext) => throw new NotSupportedException();
    public void StopFinding(bool clearSelection) => throw new NotSupportedException();
    public UIElement AsUIElement() => throw new NotSupportedException();
    public void ShowDevTools() => throw new NotSupportedException();
    public void CloseDevTools() => throw new NotSupportedException();
    public void Dispose() { }

    public record ClientApiInvocation(string Api, string? Arguments);
    public record RegisteredContentPageApi(string Name, BackendApi Api);
}