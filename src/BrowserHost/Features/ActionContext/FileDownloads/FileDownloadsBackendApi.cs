using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public record DownloadItemDto(int Id, string FileName, int Progress, bool IsCompleted, bool IsCancelled);

public class FileDownloadsBackendApi(PubSub pubSub) : BackendApi
{
    public void CancelDownload(int downloadId) =>
    pubSub.Send(new CancelDownloadCommand(downloadId));
}
