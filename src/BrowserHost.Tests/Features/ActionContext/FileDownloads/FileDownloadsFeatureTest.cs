using BrowserHost.Features.ActionContext.FileDownloads;
using CefSharp;

namespace BrowserHost.Tests.Features.ActionContext.FileDownloads;

public class FileDownloadsFeatureTest
{
    [Fact]
    public void Cancelling_a_download_publishes_a_download_cancelled_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildFileDownloadsFeature();

        context.PubSub.Send(new CancelDownloadCommand(42));

        Assert.Single(PubSubMessages.OfType<DownloadCancelledEvent>(), e => e.DownloadId == 42);
    }

    [Fact]
    public void Cancelling_an_active_download_invokes_the_callback_cancel()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .BuildFileDownloadsFeature();
        var callback = new FakeDownloadItemCallback();
        var downloadItem = CreateDownloadItem(id: 42, suggestedFileName: "file.txt", percentComplete: 10, isComplete: false, isCancelled: false);

        feature.OnDownloadUpdated(42, downloadItem, callback);
        context.PubSub.Send(new CancelDownloadCommand(42));

        Assert.True(callback.CancelCalled);
        Assert.Single(PubSubMessages.OfType<DownloadCancelledEvent>(), e => e.DownloadId == 42);
    }

    private sealed class FakeDownloadItemCallback : IDownloadItemCallback
    {
        public bool CancelCalled { get; private set; }
        public bool IsDisposed { get; private set; }
        public void Cancel() => CancelCalled = true;
        public void Dispose() => IsDisposed = true;
        public void Pause() { }
        public void Resume() { }
    }

    private static DownloadItem CreateDownloadItem(int id, string suggestedFileName, int percentComplete, bool isComplete, bool isCancelled) =>
        new()
        {
            Id = id,
            SuggestedFileName = suggestedFileName,
            PercentComplete = percentComplete,
            IsComplete = isComplete,
            IsCancelled = isCancelled,
            ContentDisposition = $"filename={suggestedFileName}"
        };
}
