using BrowserHost.CefInfrastructure;
using System.Threading.Tasks;
using System.Windows;

namespace BrowserHost.Tab;

/// <summary>
/// Abstracts the TabBrowser functionality.
/// </summary>
public interface ITabBrowser
{
    string Id { get; }
    string? CurrentDomain { get; }
    string Address { get; }

    event DependencyPropertyChangedEventHandler? AddressChanged;

    Task ExecuteScriptAsync(string script);

    void RegisterContentPageApi(BackendApi api, string name);

    bool IsLoading { get; }

    // Dev Tools

    bool HasDevTools { get; }
    void ShowDevTools();
    void CloseDevTools();

    // Zoom

    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();

    // Find Text

    void Find(string searchText, bool forward, bool matchCase, bool findNext);
    void StopFinding(bool clearSelection);
}
