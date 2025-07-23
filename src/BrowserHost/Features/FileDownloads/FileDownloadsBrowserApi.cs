using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.FileDownloads;

public record DownloadItemDto(int Id, string FileName, int Progress, bool IsCompleted, bool IsCancelled);

public record DownloadCancelledEvent(int DownloadId);

public class FileDownloadsBrowserApi : BrowserApi
{
    public void CancelDownload(int downloadId) =>
        PubSub.Publish(new DownloadCancelledEvent(downloadId));
}
