using BrowserHost.Features.Tabs;
using BrowserHost.Utilities;
using CefSharp;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace BrowserHost.Features.FileDownload;

public class FileDownloadFeature : Feature<ActionsHostBrowserApi>
{
    private readonly ConcurrentDictionary<int, DownloadInfo> _activeDownloads = new();
    private readonly Timer _progressTimer;

    public FileDownloadFeature(MainWindow window) : base(window, window.ActionsHost.Api)
    {
        _progressTimer = new Timer(SendProgressUpdate, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    public override void Register()
    {
        // Subscribe to download cancellation events from frontend
        PubSub.Subscribe<DownloadCancelledEvent>(e => CancelDownload(e.DownloadId));
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e) => false;

    private void CancelDownload(string downloadId)
    {
        if (int.TryParse(downloadId, out int id))
        {
            if (_activeDownloads.TryGetValue(id, out var downloadInfo))
            {
                downloadInfo.Callback?.Cancel();
                downloadInfo.IsCancelled = true;

                // Clean up temporary files if needed
                if (File.Exists(downloadInfo.FilePath))
                {
                    try { File.Delete(downloadInfo.FilePath); } catch { }
                }
            }
        }
    }

    private void SendProgressUpdate(object? state)
    {
        try
        {
            var downloads = _activeDownloads.Values
                .Select(d => new DownloadItemDto(
                    d.Id.ToString(),
                    Path.GetFileName(d.FilePath) ?? "Unknown",
                    d.Progress,
                    d.IsCompleted,
                    d.IsCancelled))
                .ToArray();

            if (downloads.Length > 0)
            {
                // Send to frontend through the browser
                Window.ActionsHost.UpdateDownloads(downloads);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending download progress update: {ex.Message}");
        }
    }

    // This would be called by a custom download handler
    public void OnDownloadUpdated(int downloadId, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        var downloadInfo = _activeDownloads.GetOrAdd(downloadId, _ => new DownloadInfo
        {
            Id = downloadId,
            FilePath = downloadItem.FullPath ?? "",
            Callback = callback
        });

        downloadInfo.Progress = (int)downloadItem.PercentComplete;
        downloadInfo.IsCompleted = downloadItem.IsComplete;

        if (downloadItem.IsComplete || downloadItem.IsCancelled)
        {
            // Keep completed downloads for 10 seconds (handled by frontend)
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
            {
                _activeDownloads.TryRemove(downloadId, out var _);
            });
        }
    }
}

internal class DownloadInfo
{
    public int Id { get; set; }
    public string FilePath { get; set; } = "";
    public int Progress { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsCancelled { get; set; }
    public IDownloadItemCallback? Callback { get; set; }
}