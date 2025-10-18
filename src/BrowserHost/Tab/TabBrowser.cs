using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.Settings;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Tab.CefSharp;
using BrowserHost.Tab.WebView2;
using BrowserHost.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BrowserHost.Tab;

public class TabBrowser : UserControl
{
    private record PersistableState(string Address, string? Favicon, string? Title);

    private ITabWebBrowser _browser;
    private readonly ActionContextBrowser _actionContextBrowser;
    private readonly bool _isChildBrowser;
    private PersistableState? _persistableState;

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

    public event EventHandler? PageLoadEnded;
    public bool CanGoBack => _browser.CanGoBack;
    public bool CanGoForward => _browser.CanGoForward;
    public bool HasDevTools => _browser.HasDevTools;

    public TabBrowser(string id, string address, ActionContextBrowser actionContextBrowser, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        _actionContextBrowser = actionContextBrowser;
        _isChildBrowser = isChildBrowser;
        favicon ??= FileFaviconProvider.TryGetFaviconForAddress(address);
        _browser = CreateBrowser(id, address, setManualAddress, favicon, isChildBrowser);
        Content = _browser.AsUIElement();
        AttachBrowserEvents();
    }

    private ITabWebBrowser CreateBrowser(string id, string address, bool setManualAddress, string? favicon, bool isChildBrowser)
    {
        var isSsoDomain = ShouldUseWebView2(address);
        if (isSsoDomain)
            return new WebView2Browser(id, address, _actionContextBrowser, setManualAddress, favicon, isChildBrowser);
        return new CefSharpTabBrowserAdapter(id, address, _actionContextBrowser, setManualAddress, favicon, isChildBrowser);
    }

    public void SavePersistableState()
    {
        _persistableState = new PersistableState(_browser.Address, _browser.Favicon, _browser.Title);
    }

    public string GetAddressToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations) =>
        isBookmarkedOrPinned && tabCustomizations.DisableFixedAddress != true ? _persistableState?.Address ?? _browser.Address : _browser.Address;

    public string GetTitleToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations) =>
        isBookmarkedOrPinned && tabCustomizations.DisableFixedAddress != true ? _persistableState?.Title ?? _browser.Title : _browser.Title;

    public string? GetFaviconToPersist(bool isBookmarkedOrPinned, TabCustomizationDataV1 tabCustomizations) =>
        isBookmarkedOrPinned && tabCustomizations.DisableFixedAddress != true ? _persistableState?.Favicon ?? _browser.Favicon : _browser.Favicon;

    private static bool ShouldUseWebView2(string address)
    {
        if (ContentServer.IsContentServerUrl(address)) return false;
        return SettingsFeature.ExecutionSettings.SsoEnabledDomains?.Any(domain => HasDomain(address, domain)) == true;
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
        _browser.PageLoadEnded += OnBrowserPageLoadEnded;
    }

    private void DetachBrowserEvents()
    {
        _browser.AddressChanged -= OnBrowserAddressChanged;
        _browser.PageLoadEnded -= OnBrowserPageLoadEnded;

    }

    private void OnBrowserPageLoadEnded(object? sender, EventArgs e)
    {
        PageLoadEnded?.Invoke(this, e);
    }

    private void OnBrowserAddressChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _addressChanged?.Invoke(this, e);

        if (_browser is CefSharpTabBrowserAdapter && e.NewValue is string newAddress)
        {
            if (ShouldUseWebView2(newAddress))
            {
                UpgradeToWebView2(newAddress);
            }
            else if (
                SettingsFeature.ExecutionSettings.AutoAddSsoDomains == true &&
                IsSsoLoginPage(newAddress) &&
                e.OldValue is string oldAddress &&
                Uri.TryCreate(oldAddress, UriKind.Absolute, out var oldUri) &&
                !string.IsNullOrEmpty(oldUri.Host) &&
                !ContentServer.IsContentServerUrl(oldAddress))
            {
                UpgradeToWebView2(oldAddress);
                PubSub.Publish(new SsoFlowStartedEvent(Id, oldUri.Host, oldAddress));
                return; // We restored the old address, so no further processing is needed
            }
        }

        // If navigating to a file address, set a file-type favicon immediately if available
        if (!_isChildBrowser && e.NewValue is string newAddr)
        {
            var fileFav = FileFaviconProvider.TryGetFaviconForAddress(newAddr);
            if (!string.IsNullOrEmpty(fileFav))
            {
                _actionContextBrowser.UpdateTabFavicon(Id, fileFav);
            }
        }
    }

    private static bool IsSsoLoginPage(string address) =>
        Uri.TryCreate(address, UriKind.Absolute, out var toUri) &&
        string.Equals(toUri.Host, "login.microsoftonline.com", StringComparison.OrdinalIgnoreCase);

    private void UpgradeToWebView2(string targetAddress)
    {
        var setManual = _browser.ManualAddress == targetAddress;
        var favicon = _browser.Favicon;
        var id = _browser.Id;

        DetachBrowserEvents();
        var old = _browser;
        _browser = new WebView2Browser(id, targetAddress, _actionContextBrowser, setManualAddress: setManual, favicon, _isChildBrowser);
        Content = _browser.AsUIElement();
        AttachBrowserEvents();
        old.Dispose();
    }

    public void RestoreOriginalAddress()
    {
        if (_persistableState != null && _browser.Address != _persistableState?.Address)
            SetAddress(_persistableState!.Address, setManualAddress: false);
    }

    public void SetAddress(string address, bool setManualAddress) => _browser.SetAddress(address, setManualAddress);
    public void RegisterContentPageApi(BrowserApi api, string name) => _browser.RegisterContentPageApi(api, name);
    public void Reload(bool ignoreCache = false) => _browser.Reload(ignoreCache);
    public void Dispose()
    {
        try { DetachBrowserEvents(); } catch { }
        _browser.Dispose();
    }
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
    public Task ExecuteScriptAsync(string script) => _browser.ExecuteScriptAsync(script);
}
