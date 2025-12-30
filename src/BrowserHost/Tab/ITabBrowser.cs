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

    event DependencyPropertyChangedEventHandler? AddressChanged;

    Task ExecuteScriptAsync(string script);

    // Zoom

    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();

    // Find Text

    void Find(string searchText, bool forward, bool matchCase, bool findNext);
    void StopFinding(bool clearSelection);
}
