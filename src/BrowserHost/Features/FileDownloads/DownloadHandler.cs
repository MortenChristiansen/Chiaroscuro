using CefSharp;
using System.IO;

namespace BrowserHost.Features.FileDownloads;

public class DownloadHandler(string downloadDirectory) : IDownloadHandler
{
    public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
    {
        return true;
    }

    public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
    {
        if (!callback.IsDisposed)
        {
            var filePath = Path.Combine(downloadDirectory, downloadItem.SuggestedFileName);
            callback.Continue(filePath, showDialog: false);
        }

        // Return true to indicate the event was handled
        return true;
    }

    public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        var fileDownloadFeature = MainWindow.Instance.GetFeature<FileDownloadsFeature>();
        fileDownloadFeature.OnDownloadUpdated(downloadItem.Id, downloadItem, callback);
    }
}
