using EmbedIO;
using EmbedIO.Files;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BrowserHost;

static class ContentServer
{
    private const string _host = "http://localhost:9696";

    public static void Run()
    {
        var server = CreateWebServer();
        Task.Run(async () =>
        {
            await server.RunAsync();
        });
    }

    public static string GetUiAddress(string path) =>
        _host + path;

    private static WebServer CreateWebServer()
    {
        // Determine the path to the chrome-app folder in the output directory
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var chromeAppRoot = Path.Combine(baseDir, "chrome-app");
        var chromeAppActionDialog = Path.Combine(baseDir, "chrome-app", "action-dialog");

        return new WebServer(o => o
            .WithUrlPrefix(_host)
            .WithMode(HttpListenerMode.EmbedIO)
        )
        .WithStaticFolder("/", chromeAppRoot, true, m => m.WithContentCaching())
        .WithStaticFolder("/action-dialog", chromeAppActionDialog, true, m => m.WithContentCaching())
        ;
    }


}
