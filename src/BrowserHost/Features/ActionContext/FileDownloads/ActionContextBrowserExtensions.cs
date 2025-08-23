using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public static class ActionContextBrowserExtensions
{
    public static void UpdateDownloads(this ActionContextBrowser browser, DownloadItemDto[] downloads)
    {
        browser.CallClientApi("downloadsChanged", downloads.ToJsonObject());
    }
}
