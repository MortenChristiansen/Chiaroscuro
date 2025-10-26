#if !DEBUG
using EmbedIO;
using EmbedIO.Files;
using System.IO;
using System.Threading.Tasks;
#endif

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BrowserHost;

public record ContentPage(string Address, string Title, string Favicon);

public enum ContentPageUrlMode
{
    Relative,
    Absolute
}

static class ContentServer
{
    private const string SettingsFavicon = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64' viewBox='0 0 64 64'><defs><linearGradient id='g' x1='0' y1='0' x2='0' y2='1'><stop offset='0%' stop-color='%23f8fafc'/><stop offset='100%' stop-color='%23e2e8f0'/></linearGradient></defs><circle cx='32' cy='32' r='30' fill='url(%23g)' stroke='%2394a3b8' stroke-width='2'/><g transform='translate(32,32)'><g fill='%23565f7a'><circle r='8' fill='%238ea2b8'/><g stroke='%23565f7a' stroke-width='4' stroke-linecap='round'><line x1='0' y1='-18' x2='0' y2='-26'/><line x1='0' y1='18' x2='0' y2='26'/><line x1='18' y1='0' x2='26' y2='0'/><line x1='-18' y1='0' x2='-26' y2='0'/><line x1='12.7' y1='12.7' x2='18.4' y2='18.4'/><line x1='-12.7' y1='-12.7' x2='-18.4' y2='-18.4'/><line x1='12.7' y1='-12.7' x2='18.4' y2='-18.4'/><line x1='-12.7' y1='12.7' x2='-18.4' y2='18.4'/></g></g></g></svg>";

#if DEBUG
    private const string _host = "http://localhost:4200";
#else
    private const string _host = "http://localhost:9696";
#endif

    public static void Run()
    {
#if !DEBUG
        var server = CreateWebServer();
        Task.Run(async () =>
        {
            await server.RunAsync();
        });
#endif
    }

    public static string GetUiAddress(string path) =>
        _host + path;

    public static bool IsContentServerUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;
        return url.StartsWith(_host, StringComparison.OrdinalIgnoreCase);
    }

    // Note that this information is duplicated in app.routes.ts
    public static readonly ContentPage[] Pages = [new("/settings", "Settings", SettingsFavicon)];

    public static bool IsContentPage(string url, [NotNullWhen(true)] out ContentPage? contentPage, ContentPageUrlMode urlMode = ContentPageUrlMode.Relative)
    {
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
    private static WebServer CreateWebServer()
    {
        // Determine the path to the chrome-app folder in the output directory
        var baseDir = AppContext.BaseDirectory;
        var chromeAppRoot = Path.Combine(baseDir, "chrome-app");
        var chromeAppActionDialog = Path.Combine(baseDir, "chrome-app", "action-dialog");
        var tabs = Path.Combine(baseDir, "chrome-app", "tabs");

        return new WebServer(o => o
            .WithUrlPrefix(_host)
            .WithMode(HttpListenerMode.EmbedIO)
        )
        .WithStaticFolder("/", chromeAppRoot, true, m => m.WithContentCaching())
        .WithStaticFolder("/action-dialog", chromeAppActionDialog, true, m => m.WithContentCaching())
        .WithStaticFolder("/action-context", tabs, true, m => m.WithContentCaching())
        .WithStaticFolder("/tab-palette", tabs, true, m => m.WithContentCaching())
        .WithStaticFolder("/context-menu", tabs, true, m => m.WithContentCaching())
        .WithStaticFolder("/settings", tabs, true, m => m.WithContentCaching())
        ;
    }
#endif

}
