using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserHost.Features.ActionContext.FileDownloads;

public class FileDownloadsFeature(MainWindow window) : Feature(window)
{
    private readonly ConcurrentDictionary<int, DownloadInfo> _activeDownloads = new();
    private Timer? _progressTimer;

    public override void Configure()
    {
        PubSub.Subscribe<DownloadCancelledEvent>(HandleFileDownloadCancelled);
        PubSub.Subscribe<BackgroundDownloadStartedEvent>(OnBackgroundDownloadStarted);
    }

    private void HandleFileDownloadCancelled(DownloadCancelledEvent e)
    {
        if (_activeDownloads.TryRemove(e.DownloadId, out var downloadInfo) && !downloadInfo.IsCompleted)
            downloadInfo.Cancel?.Invoke();
    }

    private int _nextBackgroundDownloadId = 1_000_000;
    private async Task OnBackgroundDownloadStarted(BackgroundDownloadStartedEvent e)
    {
        EnsureDownloadTimerCreated();

        var downloadId = Interlocked.Increment(ref _nextBackgroundDownloadId);
        var ct = new CancellationTokenSource();
        var downloadInfo = new DownloadInfo
        {
            Id = downloadId,
            FileName = e.FileName,
            Cancel = ct.Cancel,
            IsCancelled = false,
            IsCompleted = false,
            Progress = 0
        };

        if (!_activeDownloads.TryAdd(downloadId, downloadInfo))
            return;

        SendProgressUpdate();

        var data = await DownloadHelper.DownloadBytesAsync(e.DownloadSource, progress =>
        {
            downloadInfo.Progress = progress.PercentCompleted;
            downloadInfo.IsCompleted = progress.HasCompleted;
            SendProgressUpdate();

            if (progress.HasCompleted || progress.IsCancelled)
                RemoveCompletedDownloadAfterDelay(downloadId);
        },
        ct.Token);

        if (data != null)
            await DownloadHelper.SaveFile(e.FileName, data);
    }

    public void OnDownloadUpdated(int downloadId, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        EnsureDownloadTimerCreated();

        var fileName = !string.IsNullOrWhiteSpace(downloadItem.SuggestedFileName) ?
            downloadItem.SuggestedFileName :
            downloadItem.ContentDisposition.Split("filename=").Last();

        var downloadInfo = _activeDownloads.GetOrAdd(downloadId, _ => new DownloadInfo
        {
            Id = downloadId,
            FileName = fileName,
            Cancel = callback.Cancel,
            IsCancelled = false,
            IsCompleted = false,
            Progress = 0
        });

        downloadInfo.Progress = downloadItem.PercentComplete;
        downloadInfo.IsCompleted = downloadItem.IsComplete;
        downloadInfo.IsCancelled = downloadItem.IsCancelled;

        if (downloadItem.IsComplete || downloadItem.IsCancelled)
            RemoveCompletedDownloadAfterDelay(downloadId);
    }

    private void RemoveCompletedDownloadAfterDelay(int downloadId)
    {
        // Keep completed downloads for 10 seconds
        var _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
        {
            _activeDownloads.TryRemove(downloadId, out var _);
            if (_activeDownloads.Count == 0)
            {
                _progressTimer?.Dispose();
                _progressTimer = null;
                SendProgressUpdate();
            }
        });
    }

    private void EnsureDownloadTimerCreated()
    {
        _progressTimer ??= new Timer(SendProgressUpdate, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    private void SendProgressUpdate(object? state = null)
    {
        var downloads = _activeDownloads.Values
            .Select(d => new DownloadItemDto(
                d.Id,
                d.FileName,
                d.Progress,
                d.IsCompleted,
                d.IsCancelled))
            .ToArray();

        Window.ActionContext.UpdateDownloads(downloads);
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
                download.Cancel?.Invoke();
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
    public required Action? Cancel { get; set; }
}