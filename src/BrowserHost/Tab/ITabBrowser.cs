using System.Threading.Tasks;

namespace BrowserHost.Tab;

public interface ITabBrowser
{
    string Id { get; }
    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();
}
