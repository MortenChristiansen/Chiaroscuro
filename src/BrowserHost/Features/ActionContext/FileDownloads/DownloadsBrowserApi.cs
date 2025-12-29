using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public class DownloadsBrowserApi(BaseBrowser actionContextBrowser) : BrowserApi(actionContextBrowser)
{
    public void UpdateDownloads(DownloadItemDto[] downloads) =>
        CallClientApi("downloadsChanged", downloads.ToJsonObject());
}
