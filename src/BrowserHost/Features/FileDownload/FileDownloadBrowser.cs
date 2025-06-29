using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using CefSharp;

namespace BrowserHost.Features.FileDownload;

public class FileDownloadBrowser : Browser<FileDownloadBrowserApi>
{
    public override FileDownloadBrowserApi Api { get; }

    public FileDownloadBrowser()
        : base("/actions-host")
    {
        Api = new FileDownloadBrowserApi(this);
    }

    public void UpdateDownloads(DownloadItemDto[] downloads)
    {
        RunWhenSourceHasLoaded(() =>
        {
            var script = $"window.angularApi.downloadsChanged({downloads.ToJsonObject()})";
            this.ExecuteScriptAsync(script);
        });
    }
}