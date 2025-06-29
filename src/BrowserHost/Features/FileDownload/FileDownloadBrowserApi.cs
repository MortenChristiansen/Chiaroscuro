using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.FileDownload;

public record DownloadProgressChangedEvent(DownloadItemDto[] Downloads);
public record DownloadCancelledEvent(string DownloadId);

public record DownloadItemDto(string Id, string FileName, int Progress, bool IsCompleted, bool IsCancelled);

public class FileDownloadBrowserApi(FileDownloadBrowser browser) : BrowserApi(browser)
{
    public void CancelDownload(string downloadId) =>
        PubSub.Publish(new DownloadCancelledEvent(downloadId));

    public void DownloadsChanged(List<object> downloads) =>
        PubSub.Publish(new DownloadProgressChangedEvent(
            [.. downloads.Select((dynamic download) => new DownloadItemDto(download.Id, download.FileName, download.Progress, download.IsCompleted, download.IsCancelled))]
        ));
}