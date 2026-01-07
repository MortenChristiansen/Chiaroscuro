using BrowserHost.Features.ActionContext.FileDownloads;
using CefSharp;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BrowserHost.Tests.Features.ActionContext.FileDownloads;

public class FileDownloadsFeatureTest
{
    [Fact]
    public void Cancelling_a_download_publishes_a_download_cancelled_event()
    {
        CreateFeature
            .CaptureContext(out var context)
            .BuildFileDownloadsFeature();
        PubSubMessages.Clear();

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
        PubSubMessages.Clear();

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

    private static DownloadItem CreateDownloadItem(int id, string suggestedFileName, int percentComplete, bool isComplete, bool isCancelled)
    {
        var item = (DownloadItem)RuntimeHelpers.GetUninitializedObject(typeof(DownloadItem));

        Set(item, "Id", id);
        Set(item, "SuggestedFileName", suggestedFileName);
        Set(item, "ContentDisposition", $"filename={suggestedFileName}");
        Set(item, "PercentComplete", percentComplete);
        Set(item, "IsComplete", isComplete);
        Set(item, "IsCancelled", isCancelled);

        return item;
    }

    private static void Set<T>(T instance, string name, object? value) where T : class
    {
        var type = instance.GetType();
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        var property = type.GetProperty(name, flags);
        if (property != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField(name, flags);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        var backingField = type.GetField($"<{name}>k__BackingField", flags);
        if (backingField != null)
        {
            backingField.SetValue(instance, value);
            return;
        }

        throw new InvalidOperationException($"Could not set {type.Name}.{name}");
    }
}
