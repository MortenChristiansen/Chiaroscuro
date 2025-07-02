namespace BrowserHost.Features.FileDownloads;

public class FileDownloadsFeature(MainWindow window) : Feature<FileDownloadsBrowserApi>(window, window.ActionContext.FileDownloadsApi)
{
    public override void Register()
    {
    }
}
