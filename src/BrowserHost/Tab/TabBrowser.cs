using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.Settings;
using BrowserHost.Tab.CefSharp;
using BrowserHost.Tab.WebView2;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BrowserHost.Tab;

public class TabBrowser : UserControl
{
    private ITabWebBrowser _browser;
    private readonly ActionContextBrowser _actionContextBrowser;
    private readonly string[] _ssoDomains;

    private event DependencyPropertyChangedEventHandler? _addressChanged;
    public event DependencyPropertyChangedEventHandler? AddressChanged
    {
        add => _addressChanged += value;
        remove => _addressChanged -= value;
    }

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

    public TabBrowser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon)
    {
        _actionContextBrowser = actionContextBrowser;
        _ssoDomains = SettingsFeature.ExecutionSettings.SsoEnabledDomains ?? [];
        _browser = CreateBrowser(id, address, setManualAddress, favicon);
        Content = _browser.AsUIElement();
        AttachBrowserEvents();
    }

    private ITabWebBrowser CreateBrowser(string id, string address, bool setManualAddress, string? favicon)
    {
        var isSsoDomain = ShouldUseWebView2(address);
        if (isSsoDomain)
            return new WebView2Browser(id, address, _actionContextBrowser, setManualAddress, favicon);
        return new CefSharpTabBrowserAdapter(id, address, _actionContextBrowser, setManualAddress, favicon);
    }

    private bool ShouldUseWebView2(string address)
    {
        if (ContentServer.IsContentServerUrl(address)) return false;
        return _ssoDomains.Any(domain => HasDomain(address, domain));
    }

    private static bool HasDomain(string address, string domain)
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(domain)) return false;
        // Ensure absolute URI for reliable parsing
        var candidate = address.Contains("://", StringComparison.Ordinal) ? address : "https://" + address;
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.TrimEnd('.').ToLowerInvariant();
        var d = domain.Trim().TrimStart('.').ToLowerInvariant();
        // Allow exact host or subdomain match on a dot boundary
        return host == d || host.EndsWith("." + d, StringComparison.Ordinal);
    }

    private void AttachBrowserEvents()
    {
        _browser.AddressChanged += OnBrowserAddressChanged;
    }

    private void DetachBrowserEvents()
    {
        _browser.AddressChanged -= OnBrowserAddressChanged;
    }

    private void OnBrowserAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _addressChanged?.Invoke(this, e);

        if (_browser is CefSharpTabBrowserAdapter && e.NewValue is string newAddress && ShouldUseWebView2(newAddress))
        {
            UpgradeToWebView2(newAddress);
        }
    }

    private void UpgradeToWebView2(string targetAddress)
    {
        var setManual = _browser.ManualAddress == targetAddress;
        var favicon = _browser.Favicon;
        var id = _browser.Id;

        DetachBrowserEvents();
        var old = _browser;
        _browser = new WebView2Browser(id, targetAddress, _actionContextBrowser, setManualAddress: setManual, favicon);
        Content = _browser.AsUIElement();
        AttachBrowserEvents();
        old.Dispose();
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
