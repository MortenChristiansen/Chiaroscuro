using System.IO;
using System.Net;
using System.Net.Sockets;
using Xunit.Sdk;
using Xunit.v3;

namespace BrowserHost.E2E.Infrastructure;

public sealed class E2EContentServerFixture : ITestPipelineStartup
{
    private IDisposable? _server;

    private string _hostForTests { get; set; } = "";

    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        var chromeAppRoot = Path.Combine(AppContext.BaseDirectory, "chrome-app");
        _hostForTests = $"http://localhost:{GetFreeTcpPort()}";

        Environment.SetEnvironmentVariable("CHIAROSCURO_UI_HOST", _hostForTests);
        _server = ContentServer.StartStaticServerForTests(chromeAppRoot, _hostForTests);

        return ValueTask.CompletedTask;
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

    public ValueTask StopAsync()
    {
        try { _server?.Dispose(); } catch { }
        _server = null;

        return ValueTask.CompletedTask;
    }
}
