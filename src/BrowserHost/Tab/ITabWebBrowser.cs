using BrowserHost.CefInfrastructure;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserHost.Tab;

public interface ITabWebBrowser : IDisposable
{
    string Id { get; }
    string? Favicon { get; }
    string? ManualAddress { get; }
    string Address { get; }
    string Title { get; set; }
    bool IsLoading { get; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    bool HasDevTools { get; }
    double DefaultZoomLevel { get; }

    event DependencyPropertyChangedEventHandler? AddressChanged;

    void SetAddress(string address, bool setManualAddress);
    void RegisterContentPageApi(BrowserApi api, string name);
    void Reload(bool ignoreCache = false);
    void Back();
    void Forward();
    Task CallClientApi(string api, string? arguments = null);
    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void Find(string searchText, bool forward, bool matchCase, bool findNext);
    void StopFinding(bool clearSelection);
    UIElement AsUIElement();
    void ShowDevTools();
    void CloseDevTools();
}
