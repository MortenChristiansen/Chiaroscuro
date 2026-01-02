using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public record DownloadItemDto(int Id, string FileName, int Progress, bool IsCompleted, bool IsCancelled);

public record CancelDownloadCommand(int DownloadId) : ICommand;
public record StartBackgroundDownloadCommand(string DownloadSource, string FileName) : ICommand;

public record DownloadCancelledEvent(int DownloadId) : IEvent;
public record BackgroundDownloadStartedEvent(string DownloadSource, string FileName) : IEvent;

public class FileDownloadsBackendApi(PubSub pubSub) : BackendApi
{
    public void CancelDownload(int downloadId) =>
    pubSub.Send(new CancelDownloadCommand(downloadId));
}
