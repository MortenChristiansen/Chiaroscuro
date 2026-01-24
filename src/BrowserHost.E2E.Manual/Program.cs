using BrowserHost.E2E.Infrastructure;
using System;
using System.Windows;

namespace BrowserHost.E2E.Manual;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown,
        };

        using var host = E2EApplicationHost.Create();
        host.MainWindow.Closed += (_, __) => app.Shutdown();

        app.Run();
    }
}
