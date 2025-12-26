using System.Threading.Tasks;

namespace BrowserHost.Tab;

/// <summary>
/// Abstracts the TabBrowser functionality.
/// </summary>
public interface ITabBrowser
{
    string Id { get; }
    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();
}
