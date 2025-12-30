using System.Threading.Tasks;

namespace BrowserHost.Tab;

/// <summary>
/// Abstracts the TabBrowser functionality.
/// </summary>
public interface ITabBrowser
{
    string Id { get; }

    // Zoom

    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();

    // Find Text

    void Find(string searchText, bool forward, bool matchCase, bool findNext);
    void StopFinding(bool clearSelection);
}
