#if !DEBUG
using EmbedIO;
using EmbedIO.Files;
using System;
using System.IO;
using System.Threading.Tasks;
#endif

using System;
using System.Linq;

namespace BrowserHost;

public record ContentPage(string Address, string Title, string Favicon);

static class ContentServer
{
    private const string SettingsFavicon = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32' fill='none'%3E%3Ccircle cx='16' cy='16' r='8' stroke='%23666' stroke-width='2' fill='white'/%3E%3Cpath d='M16 6V2M16 30v-4M6 16H2M30 16h-4M8.22 8.22l-2.83-2.83M26.61 26.61l-2.83-2.83M8.22 23.78l-2.83 2.83M26.61 5.39l-2.83 2.83' stroke='%23666' stroke-width='2' stroke-linecap='round'/%3E%3C/svg%3E";

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

    public static readonly ContentPage[] Pages = [new("/settings", "Settings", SettingsFavicon)];

    public static bool IsContentPage(string url) =>
        Pages.Select(p => p.Address).Contains(url.Trim(), StringComparer.OrdinalIgnoreCase);

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
        .WithStaticFolder("/settings", tabs, true, m => m.WithContentCaching())
        ;
    }
#endif

}
