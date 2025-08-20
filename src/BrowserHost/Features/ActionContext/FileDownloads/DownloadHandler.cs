using CefSharp;
using System.IO;

namespace BrowserHost.Features.ActionContext.FileDownloads;

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
            var filePath = GetDisambiguatedFilePath(downloadItem);
            callback.Continue(filePath, showDialog: false);
        }

        // Return true to indicate the event was handled
        return true;
    }

    private string GetDisambiguatedFilePath(DownloadItem downloadItem)
    {
        var fileName = downloadItem.SuggestedFileName;
        var filePath = Path.Combine(downloadDirectory, fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        int count = 1;
        while (File.Exists(filePath))
        {
            fileName = $"{baseName} ({count++}){ext}";
            filePath = Path.Combine(downloadDirectory, fileName);
        }

        return filePath;
    }

    public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        var fileDownloadFeature = MainWindow.Instance.GetFeature<FileDownloadsFeature>();
        fileDownloadFeature.OnDownloadUpdated(downloadItem.Id, downloadItem, callback);
    }
}
