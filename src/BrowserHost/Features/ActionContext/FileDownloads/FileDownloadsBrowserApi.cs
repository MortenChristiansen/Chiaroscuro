using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public record DownloadItemDto(int Id, string FileName, int Progress, bool IsCompleted, bool IsCancelled);

public record DownloadCancelledEvent(int DownloadId);
public record BackgroundDownloadStartedEvent(string DownloadSource, string FileName);

public class FileDownloadsBrowserApi : BrowserApi
{
    public void CancelDownload(int downloadId) =>
        PubSub.Instance.Publish(new DownloadCancelledEvent(downloadId));
}
