using BrowserHost.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace BrowserHost.Utilities;

public record DownloadProgress(long BytesRead, long TotalBytes, int PercentCompleted, bool HasCompleted, bool IsCancelled);

public static class DownloadHelper
{
    private static readonly HttpClient _httpClient = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2), // Rotate connections for DNS refresh
            MaxConnectionsPerServer = 20,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1)
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("ChiaroscuroBrowser/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("image/avif,image/webp,image/png,image/jpeg,image/gif,*/*");

        return client;
    }

    public static async Task<byte[]?> DownloadBytesAsync(string url, Action<DownloadProgress>? progressCallback = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return null;
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType != null && !mediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase)) return null;

            var contentLength = response.Content.Headers.ContentLength ?? -1;

            // If caller didn't request progress use convenience method.
            if (progressCallback == null)
                return await response.Content.ReadAsByteArrayAsync(ct);

            using var source = await response.Content.ReadAsStreamAsync(ct);
            using var ms = contentLength > 0 ? new MemoryStream((int)Math.Min(contentLength, int.MaxValue)) : new MemoryStream();
            var buffer = new byte[81920];
            long totalRead = 0;
            progressCallback(new DownloadProgress(0, contentLength, 0, false, ct.IsCancellationRequested));
            while (true)
            {
                int read = await source.ReadAsync(buffer, ct);
                if (read == 0) break;
                ms.Write(buffer, 0, read);
                totalRead += read;
                int percent = contentLength > 0 ? (int)Math.Clamp(totalRead * 100.0 / contentLength, 0, 100) : 0;
                progressCallback(new DownloadProgress(totalRead, contentLength, percent, false, ct.IsCancellationRequested));
            }
            // Completed
            int finalPercent = contentLength > 0 ? 100 : 0;
            progressCallback(new DownloadProgress(totalRead, contentLength, finalPercent, true, ct.IsCancellationRequested));
            return ms.ToArray();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            LoggingService.Instance.Log(LogType.Info, $"DownloadUtil: failed to download '{url}': {ex.Message}");
            return null;
        }
    }

    public static async Task SaveFile(string fileName, byte[] data)
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var filePath = Path.Combine(desktopPath, fileName);
            // If file exists, append a number
            var originalFilePath = filePath;
            var count = 1;
            while (File.Exists(filePath))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
                var extension = Path.GetExtension(originalFilePath);
                filePath = Path.Combine(desktopPath, $"{fileNameWithoutExtension} ({count}){extension}");
                count++;
            }

            await File.WriteAllBytesAsync(filePath, data);
        }
        catch (IOException e)
        {
            LoggingService.Instance.LogException(e, LogType.Errors, "Error saving file: " + fileName);
        }
    }
}
