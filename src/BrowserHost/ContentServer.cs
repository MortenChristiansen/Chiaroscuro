using EmbedIO;
using EmbedIO.Files;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrowserHost;

public record ContentPage(string Address, string Title, string Favicon);

public enum ContentPageUrlMode
{
    Relative,
    Absolute
}

public static class ContentServer
{
    private const string SettingsFavicon = "FA:Settings";

    private const string HostOverrideEnvVar = "CHIAROSCURO_UI_HOST";

#if DEBUG
    private const string DefaultHost = "http://localhost:4200";
#else
    private const string DefaultHost = "http://localhost:9696";
#endif

    private static string Host
    {
        get
        {
            var overridden = Environment.GetEnvironmentVariable(HostOverrideEnvVar);
            var host = string.IsNullOrWhiteSpace(overridden) ? DefaultHost : overridden;
            return host.TrimEnd('/');
        }
    }

    public static void Run()
    {
#if !DEBUG
        var server = CreateWebServer(GetDefaultChromeAppRoot(), Host);
        Task.Run(async () =>
        {
            await server.RunAsync();
        });
#endif
    }

    public static IDisposable StartStaticServerForTests(string chromeAppRoot)
    {
        var server = CreateWebServer(chromeAppRoot, Host);

        _ = Task.Run(() => server.RunAsync())
            .ContinueWith(t => Debug.WriteLine($"ContentServer failed: {t.Exception}"), TaskContinuationOptions.OnlyOnFaulted);

        WaitUntilRunning(server, TimeSpan.FromSeconds(10));

        return server;
    }

    public static string GetUiAddress(string path) =>
        Host + path;

    public static bool IsContentServerUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;
        return url.StartsWith(Host, StringComparison.OrdinalIgnoreCase);
    }

    // Note that this information is duplicated in app.routes.ts
    public static readonly ContentPage[] Pages = [new("/settings", "Settings", SettingsFavicon)];

    public static bool IsContentPage(string url, [NotNullWhen(true)] out ContentPage? contentPage, ContentPageUrlMode urlMode = ContentPageUrlMode.Relative)
    {
        // Make sure that other /settings pages are not matched
        if (!url.StartsWith('/') && !url.StartsWith(Host + "/"))
        {
            contentPage = null;
            return false;
        }

        var adjustedUrl = urlMode switch
        {
            ContentPageUrlMode.Relative => url.Trim(),
            ContentPageUrlMode.Absolute => "/" + url.Trim().Split('/').Last(),
            _ => throw new ArgumentOutOfRangeException(nameof(urlMode), urlMode, null)
        };
        contentPage = Pages.FirstOrDefault(p => p.Address.Equals(adjustedUrl, StringComparison.OrdinalIgnoreCase));
        return contentPage != null;
    }

    public static bool IsSettingsPage(string url) =>
        IsContentPage(url, out var contentPage, ContentPageUrlMode.Absolute) &&
        contentPage.Address.Equals("/settings", StringComparison.OrdinalIgnoreCase);

#if !DEBUG
    private static string GetDefaultChromeAppRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "chrome-app");
    }
#endif

    private static WebServer CreateWebServer(string chromeAppRoot, string host)
    {
        var urlPrefix = host.TrimEnd('/') + "/";

        var chromeAppActionDialog = Path.Combine(chromeAppRoot, "action-dialog");
        var chromeAppActionContext = Path.Combine(chromeAppRoot, "action-context");
        var chromeAppTabPalette = Path.Combine(chromeAppRoot, "tab-palette");
        var chromeAppContextMenu = Path.Combine(chromeAppRoot, "context-menu");
        var chromeAppTerminal = Path.Combine(chromeAppRoot, "terminal");
        var chromeAppSettings = Path.Combine(chromeAppRoot, "settings");

        return new WebServer(o => o
            .WithUrlPrefix(urlPrefix)
            .WithMode(HttpListenerMode.EmbedIO)
        )
        .WithStaticFolder("/", chromeAppRoot, true, m => m.WithContentCaching())
        .WithStaticFolder("/action-dialog", chromeAppActionDialog, true, m => m.WithContentCaching())
        .WithStaticFolder("/action-context", chromeAppActionContext, true, m => m.WithContentCaching())
        .WithStaticFolder("/tab-palette", chromeAppTabPalette, true, m => m.WithContentCaching())
        .WithStaticFolder("/context-menu", chromeAppContextMenu, true, m => m.WithContentCaching())
        .WithStaticFolder("/terminal", chromeAppTerminal, true, m => m.WithContentCaching())
        .WithStaticFolder("/settings", chromeAppSettings, true, m => m.WithContentCaching())
        ;
    }

    private static void WaitUntilRunning(WebServer server, TimeSpan timeout)
    {
        // If already running, return immediately.
        if (server.State == WebServerState.Listening)
            return;

        using var started = new ManualResetEventSlim(false);
        using var stopped = new ManualResetEventSlim(false);

        void onStateChanged(object? _, WebServerStateChangedEventArgs e)
        {
            if (e.NewState == WebServerState.Listening)
                started.Set();
            else if (e.NewState == WebServerState.Stopped)
                stopped.Set();
        }

        server.StateChanged += onStateChanged;
        try
        {
            // Re-check after subscription to avoid races.
            if (server.State == WebServerState.Listening)
                return;

            // If it stops before it starts listening, treat as failure.
            var signaledIndex = WaitHandle.WaitAny([started.WaitHandle, stopped.WaitHandle], timeout);
            if (signaledIndex == WaitHandle.WaitTimeout)
                throw new TimeoutException($"Timed out waiting for content server to reach state {WebServerState.Listening}.");
            if (signaledIndex == 1)
                throw new InvalidOperationException("Content server transitioned to Stopped before reaching Listening state.");
        }
        finally
        {
            server.StateChanged -= onStateChanged;
        }
    }
}
