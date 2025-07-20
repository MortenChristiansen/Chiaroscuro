using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.PIP;

public class PIPBrowser(string base64Url, double timestamp) : Browser($"/pip-player?url={base64Url}&timestamp={timestamp}")
{
}
