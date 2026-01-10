using EmbedIO;
using EmbedIO.Files;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserHost;

public record ContentPage(string Address, string Title, string Favicon);

public enum ContentPageUrlMode
{
    Relative,
    Absolute
}

static class ContentServer
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

    internal static IDisposable StartStaticServerForTests(string chromeAppRoot, string? hostOverride = null)
    {
        var host = string.IsNullOrWhiteSpace(hostOverride) ? Host : hostOverride.TrimEnd('/');
        var server = CreateWebServer(chromeAppRoot, host);
        Task.Run(async () =>
        {
            await server.RunAsync();
        });
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
        .WithStaticFolder("/settings", chromeAppSettings, true, m => m.WithContentCaching())
        ;
    }

}
