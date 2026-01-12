using System.IO;
using System.Net;
using System.Net.Sockets;
using Xunit.Sdk;
using Xunit.v3;

namespace BrowserHost.E2E.Infrastructure;

public sealed class E2ETestPipelineStartup : ITestPipelineStartup
{
    private IDisposable? _server;

    public ValueTask StartAsync(IMessageSink diagnosticMessageSink)
    {
        // We need to do it before starting the tests, otherwise there might be a file lock preventing deletion
        ClearCefFolder();

        var chromeAppRoot = FindChromeAppRoot();
        var hostForTests = $"http://localhost:{GetFreeTcpPort()}";

        Environment.SetEnvironmentVariable("CHIAROSCURO_UI_HOST", hostForTests);
        _server = ContentServer.StartStaticServerForTests(chromeAppRoot);

        return ValueTask.CompletedTask;
    }

    private static void ClearCefFolder()
    {
        var cefFolder = Path.Combine(Path.GetTempPath(), "BrowserHost.E2E");
        if (!Directory.Exists(cefFolder))
            return;

        // Delete all folders in cefFolder
        foreach (var dir in Directory.GetDirectories(cefFolder))
            Directory.Delete(dir, true);
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

    private static string FindChromeAppRoot()
    {
        // VS test runner can shadow-copy to TestResults/.../Out, so don't rely on a fixed relative path.
        // We search upwards for: src/BrowserHost/chrome-app/index.html
        // TODO: Figure out which of these things is actually necessary.

        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
        };

        foreach (var start in candidates)
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 12 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "src", "BrowserHost", "chrome-app");
                if (File.Exists(Path.Combine(candidate, "index.html")))
                    return candidate;

                dir = dir.Parent;
            }
        }

        // Fallback to the previous behavior for environments that match the original layout.
        var legacy = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BrowserHost", "chrome-app"));
        if (File.Exists(Path.Combine(legacy, "index.html")))
            return legacy;

        throw new DirectoryNotFoundException(
            "Could not locate BrowserHost/chrome-app/index.html. " +
            $"BaseDirectory='{AppContext.BaseDirectory}', CurrentDirectory='{Environment.CurrentDirectory}'.");
    }

    public ValueTask StopAsync()
    {
        try { _server?.Dispose(); } catch { }
        _server = null;

        Environment.SetEnvironmentVariable("CHIAROSCURO_UI_HOST", null);

        return ValueTask.CompletedTask;
    }
}
