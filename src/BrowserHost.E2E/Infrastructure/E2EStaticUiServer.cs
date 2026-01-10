using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BrowserHost.E2E.Infrastructure;

internal sealed class E2EStaticUiServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private readonly string _root;

    public string BaseUrl { get; }

    private E2EStaticUiServer(string baseUrl, string root, HttpListener listener)
    {
        BaseUrl = baseUrl.TrimEnd('/');
        _root = root;
        _listener = listener;
        _loop = Task.Run(ListenLoopAsync);
    }

    public static E2EStaticUiServer Start(string chromeAppRoot)
    {
        if (string.IsNullOrWhiteSpace(chromeAppRoot))
            throw new ArgumentException("chromeAppRoot must be provided", nameof(chromeAppRoot));

        if (!Directory.Exists(chromeAppRoot))
            throw new DirectoryNotFoundException($"chrome-app folder not found at '{chromeAppRoot}'. Ensure it is copied to the test output.");

        var port = GetFreeTcpPort();
        var prefix = $"http://localhost:{port}/";

        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();

        return new E2EStaticUiServer(prefix.TrimEnd('/'), chromeAppRoot, listener);
    }

    private async Task ListenLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            HttpListenerContext? ctx = null;
            try
            {
                ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleAsync(ctx), _cts.Token);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch
            {
                try { ctx?.Response.Abort(); } catch { }
            }
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;

            // Strip query string.
            var path = req.Url?.AbsolutePath ?? "/";

            // Normalize.
            if (path == "/")
            {
                await RespondNotFound(res).ConfigureAwait(false);
                return;
            }

            // /settings and /settings/ should serve the settings page.
            if (string.Equals(path, "/settings", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(path, "/settings/", StringComparison.OrdinalIgnoreCase))
            {
                var index = Path.Combine(_root, "settings", "index.html");
                await ServeFile(res, index).ConfigureAwait(false);
                return;
            }

            // Requests originating from /settings/index.html often request assets as /settings/<asset>
            // (because the HTML uses relative URLs). We map those back to the root folder.
            if (path.StartsWith("/settings/", StringComparison.OrdinalIgnoreCase))
            {
                var asset = path["/settings/".Length..];

                // Prefer settings folder if it exists there.
                var candidateInSettings = Path.Combine(_root, "settings", asset);
                if (File.Exists(candidateInSettings))
                {
                    await ServeFile(res, candidateInSettings).ConfigureAwait(false);
                    return;
                }

                var candidateInRoot = Path.Combine(_root, asset);
                if (File.Exists(candidateInRoot))
                {
                    await ServeFile(res, candidateInRoot).ConfigureAwait(false);
                    return;
                }

                await RespondNotFound(res).ConfigureAwait(false);
                return;
            }

            // Default: serve from chrome-app root.
            var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_root, relative);

            if (!File.Exists(filePath))
            {
                await RespondNotFound(res).ConfigureAwait(false);
                return;
            }

            await ServeFile(res, filePath).ConfigureAwait(false);
        }
        catch
        {
            try { ctx.Response.Abort(); } catch { }
        }
    }

    private static async Task ServeFile(HttpListenerResponse res, string filePath)
    {
        res.StatusCode = 200;
        res.ContentType = GetContentType(filePath);

        // Avoid caching issues while iterating on tests.
        res.Headers["Cache-Control"] = "no-store";

        var bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
        res.OutputStream.Close();
    }

    private static Task RespondNotFound(HttpListenerResponse res)
    {
        res.StatusCode = 404;
        res.ContentType = "text/plain";
        var bytes = Encoding.UTF8.GetBytes("Not found");
        res.ContentLength64 = bytes.Length;
        return res.OutputStream.WriteAsync(bytes, 0, bytes.Length).ContinueWith(_ =>
        {
            try { res.OutputStream.Close(); } catch { }
        });
    }

    private static string GetContentType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json; charset=utf-8",
            ".svg" => "image/svg+xml",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".ico" => "image/x-icon",
            ".woff" => "font/woff",
            ".woff2" => "font/woff2",
            _ => "application/octet-stream",
        };
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    public void Dispose()
    {
        try { _cts.Cancel(); } catch { }
        try { _listener.Stop(); } catch { }
        try { _listener.Close(); } catch { }

        try { _loop.GetAwaiter().GetResult(); } catch { }
    }
}
