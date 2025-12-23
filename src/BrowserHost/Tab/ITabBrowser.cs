using System.Threading.Tasks;

namespace BrowserHost.Tab;

public interface ITabBrowser
{
    Task<double> GetZoomLevelAsync();
    void SetZoomLevel(double level);
    void ResetZoomLevel();
}
