#if !DEBUG
using EmbedIO;
using EmbedIO.Files;
using System;
using System.IO;
using System.Threading.Tasks;
#endif

namespace BrowserHost;

static class ContentServer
{
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
        ;
    }
#endif

}
