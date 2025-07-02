using BrowserHost.CefInfrastructure;

namespace BrowserHost.Features.FileDownloads;

public class FileDownloadsBrowserApi(IBaseBrowser browser) : BrowserApi(browser)
{
}
