using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserHost.Features.FileDownloads;

public class FileDownloadsFeature(MainWindow window) : Feature<FileDownloadsBrowserApi>(window, window.ActionContext.FileDownloadsApi)
{
    private readonly ConcurrentDictionary<int, DownloadInfo> _activeDownloads = new();
    private Timer? _progressTimer;
    private int? _lastDownloadCount;

    public override void Register()
    {
        _progressTimer = new Timer(SendProgressUpdate, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        PubSub.Subscribe<DownloadCancelledEvent>(HandleFileDownloadCancelled);
    }

    private void HandleFileDownloadCancelled(DownloadCancelledEvent e)
    {
        if (_activeDownloads.TryRemove(e.DownloadId, out var downloadInfo) && !downloadInfo.IsCompleted)
            downloadInfo.Callback?.Cancel();
    }

    private void SendProgressUpdate(object? state)
    {
        var downloads = _activeDownloads.Values
            .Select(d => new DownloadItemDto(
                d.Id,
                d.FileName,
                d.Progress,
                d.IsCompleted,
                d.IsCancelled))
            .ToArray();

        if (downloads.Length > 0 || _lastDownloadCount != 0)
            Window.ActionContext.UpdateDownloads(downloads);

        _lastDownloadCount = downloads.Length;
    }

    public void OnDownloadUpdated(int downloadId, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        var fileName = !string.IsNullOrWhiteSpace(downloadItem.SuggestedFileName) ?
            downloadItem.SuggestedFileName :
            downloadItem.ContentDisposition.Split("filename=").Last();

        var downloadInfo = _activeDownloads.GetOrAdd(downloadId, _ => new DownloadInfo
        {
            Id = downloadId,
            FileName = fileName,
            Callback = callback,
            IsCancelled = false,
            IsCompleted = false,
            Progress = 0
        });

        downloadInfo.Progress = downloadItem.PercentComplete;
        downloadInfo.IsCompleted = downloadItem.IsComplete;
        downloadInfo.IsCancelled = downloadItem.IsCancelled;

        if (downloadItem.IsComplete || downloadItem.IsCancelled)
        {
            // Keep completed downloads for 10 seconds
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ => _activeDownloads.TryRemove(downloadId, out var _));
        }
    }

    public bool HasActiveDownloads()
    {
        return _activeDownloads.Values.Any(d => !d.IsCompleted && !d.IsCancelled);
    }

    public void CancelAllActiveDownloads()
    {
        foreach (var download in _activeDownloads.Values)
        {
            if (!download.IsCompleted && !download.IsCancelled)
            {
                download.Callback?.Cancel();
                download.IsCancelled = true;
            }
        }
    }
}

internal class DownloadInfo
{
    public required int Id { get; set; }
    public required string FileName { get; set; }
    public required int Progress { get; set; }
    public required bool IsCompleted { get; set; }
    public required bool IsCancelled { get; set; }
    public required IDownloadItemCallback? Callback { get; set; }
}