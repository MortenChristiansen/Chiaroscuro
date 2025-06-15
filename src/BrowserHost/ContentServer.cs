using EmbedIO;
using EmbedIO.Files;
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
        return new WebServer(o => o
            .WithUrlPrefix(_host)
            .WithMode(HttpListenerMode.EmbedIO)
        )
        .WithStaticFolder("/", "C:/Users/morten/Documents/Code/Chiaroscuro/src/chrome-app/dist/chrome-app/browser/", true, m => m.WithContentCaching())
        .WithStaticFolder("/action-dialog", "C:/Users/morten/Documents/Code/Chiaroscuro/src/chrome-app/dist/chrome-app/browser/action-dialog/", true, m => m.WithContentCaching())
        ;
    }
}
