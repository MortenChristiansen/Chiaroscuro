using BrowserHost.CefInfrastructure;
using BrowserHost.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
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
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Intentionally left blank until download integration is added.
        });
    }

    public void CopyImage(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return;

        Task.Run(async () =>
        {
            var bytes = await DownloadUtil.DownloadBytesAsync(imageUrl);

            if (bytes == null || bytes.Length == 0)
                return;

            var ms = new MemoryStream(bytes);
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    ms.Position = 0;
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    bmp.EndInit();
                    bmp.Freeze();
                    Clipboard.SetImage(bmp);
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Log(LogType.Info, $"CopyImage: failed to set clipboard for '{imageUrl}': {ex.Message}");
                }
            });
        });
    }

    private sealed class DownloadUtil
    {
        private static HttpClient CreateClient()
        {
            var c = new HttpClient();
            try
            {
                c.Timeout = TimeSpan.FromSeconds(15);
                c.DefaultRequestHeaders.UserAgent.ParseAdd("ChiaroscuroBrowser/1.0");
                c.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/png,image/jpeg,image/gif,*/*");
            }
            catch { }
            return c;
        }

        public static async Task<byte[]?> DownloadBytesAsync(string url, CancellationToken ct = default)
        {
            var httpClient = CreateClient();
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
            try
            {
                using var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode) return null;
                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType != null && !mediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase)) return null;
                return await response.Content.ReadAsByteArrayAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                LoggingService.Instance.Log(LogType.Info, $"DownloadUtil: failed to download '{url}': {ex.Message}");
                return null;
            }
        }
    }
}
