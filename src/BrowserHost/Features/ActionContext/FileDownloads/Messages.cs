using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public record CancelDownloadCommand(int DownloadId) : ICommand;
public record DownloadCancelledEvent(int DownloadId) : IEvent;

public record StartBackgroundDownloadCommand(string DownloadSource, string FileName) : ICommand;
public record BackgroundDownloadStartedEvent(string DownloadSource, string FileName) : IEvent;
