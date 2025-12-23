using BrowserHost.CefInfrastructure;
using BrowserHost.Features.ActionContext.FileDownloads;
using BrowserHost.Logging;
using BrowserHost.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuBrowserApi : BrowserApi
{
    public void DismissContextMenu()
    {
        try // Throws if already closing
        {
            Application.Current.Dispatcher.Invoke(() =>
                Application.Current.Windows.OfType<WebContextMenuWindow>().Where(w => w.IsActive).FirstOrDefault()?.Close()
            );
        }
        catch { }
    }

    public void DownloadImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            LoggingService.Instance.Log(LogType.Info, $"DownloadImage: invalid URL '{imageUrl}'");
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            var evt = new BackgroundDownloadStartedEvent(imageUrl, Path.GetFileName(uri.LocalPath));
            PubSub.Instance.Publish(evt);
        });
    }

    public void CopyImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        Task.Run(async () =>
        {
            var bytes = await DownloadHelper.DownloadBytesAsync(imageUrl);

            if (bytes == null || bytes.Length == 0)
                return;

            var ms = new MemoryStream(bytes);
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    ms.Position = 0;
                    var bmp = CreateBitmap(ms);
                    Clipboard.SetImage(bmp);
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Log(LogType.Info, $"CopyImage: failed to set clipboard for '{imageUrl}': {ex.Message}");
                }
            });
        });
    }

    private static BitmapImage CreateBitmap(MemoryStream ms)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.StreamSource = ms;
        bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }
}
