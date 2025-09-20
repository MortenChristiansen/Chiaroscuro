﻿using BrowserHost.CefInfrastructure;
using BrowserHost.Features.Settings;
using BrowserHost.Logging;
using BrowserHost.Utilities;
using CefSharp;
using CefSharp.Wpf;
using NuGet.Versioning;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Text;
using Velopack;

namespace BrowserHost;

/// <summary>
/// For .Net 5.0/6.0/7.0 Publishing Single File exe requires using your own applications executable to
/// act as the BrowserSubProcess. See https://github.com/cefsharp/CefSharp/issues/3407
/// for further details. <see cref="Program.Main(string[])"/> for the default main application entry point
/// </summary>
public class ProgramPublishSingleFile
{
    [STAThread]
    [SupportedOSPlatform("windows")]
    public static int Main(string[] args)
    {
        // Set up unhandled exception handlers for crash logging
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogCrash(ex);
                LoggingService.Instance.Dispose(); // Flush logs before crash
            }
        };
        
        VelopackApp
            .Build()
            .OnFirstRun(WindowsRegistrator.RegisterApplication)
            .OnRestarted(WindowsRegistrator.RegisterApplication)
            .Run();

        if (App.Options.ForceAppRegistration)
            WindowsRegistrator.RegisterApplication(new SemanticVersion(0, 0, 0));

        var exitCode = CefSharp.BrowserSubprocess.SelfHost.Main(args);
        if (exitCode >= 0)
            return exitCode;

        var appSettings = SettingsFeature.ExecutionSettings;

        var settings = new CefSettings()
        {
            //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache"),
            BrowserSubprocessPath = Process.GetCurrentProcess().MainModule!.FileName,
            UserAgent = appSettings.UserAgent ?? "",
        };

        //Example of setting a command line argument
        //Enables WebRTC
        // - CEF Doesn't currently support permissions on a per browser basis see https://bitbucket.org/chromiumembedded/cef/issues/2582/allow-run-time-handling-of-media-access
        // - CEF Doesn't currently support displaying a UI for media access permissions
        //
        //NOTE: WebRTC Device Id's aren't persisted as they are in Chrome see https://bitbucket.org/chromiumembedded/cef/issues/2064/persist-webrtc-deviceids-across-restart
        settings.CefCommandLineArgs.Add("enable-media-stream");
        //https://peter.sh/experiments/chromium-command-line-switches/#use-fake-ui-for-media-stream
        settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
        //For screen sharing add (see https://bitbucket.org/chromiumembedded/cef/issues/2582/allow-run-time-handling-of-media-access#comment-58677180)
        settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

        //Don't perform a dependency check
        //By default this example calls Cef.Initialize in the CefSharp.MinimalExample.Wpf.App
        //constructor for purposes of providing a self contained single file example we call it here.
        //You could remove this code and use the CefSharp.MinimalExample.Wpf.App example if you 
        //set BrowserSubprocessPath to an absolute path to your main application exe.
        Cef.Initialize(settings, performDependencyCheck: false, new BrowserProcessHandler());

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }

    private static void LogCrash(Exception exception)
    {
        var message = new StringBuilder();
        message.AppendLine($"Application crashed: {exception.Message}");
        message.AppendLine($"Exception Type: {exception.GetType().FullName}");
        message.AppendLine("Stack Trace:");
        message.AppendLine(exception.StackTrace);
        
        if (exception.InnerException != null)
        {
            message.AppendLine("Inner Exception:");
            message.AppendLine($"  {exception.InnerException.Message}");
            message.AppendLine($"  Type: {exception.InnerException.GetType().FullName}");
            message.AppendLine("  Stack Trace:");
            message.AppendLine($"  {exception.InnerException.StackTrace}");
        }
        
        LoggingService.Instance.Log(LogType.Crashes, message.ToString());
    }
}
