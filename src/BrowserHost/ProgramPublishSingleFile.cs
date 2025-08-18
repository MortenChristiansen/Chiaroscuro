using BrowserHost.Features.Settings;
using CefSharp;
using CefSharp.Wpf;
using Microsoft.Win32;
using NuGet.Versioning;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
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
        VelopackApp
            .Build()
            .OnFirstRun(RegisterApplication)
            .OnRestarted(RegisterApplication)
            .Run();

        if (args.Contains("forceAppRegistration"))
        {
            RegisterApplication(new SemanticVersion(0, 0, 0));
        }

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
        Cef.Initialize(settings, performDependencyCheck: false);

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterApplication(SemanticVersion version)
    {
        var isTestApplication = version == new SemanticVersion(0, 0, 0);

        if (!isTestApplication && !App.UpdateManager.IsInstalled)
            return;

        // Application details
        var appName = isTestApplication ? "Chiaroscuro (Test)" : "Chiaroscuro";
        var progId = isTestApplication ? "chiaroscuro-browser-test" : "chiaroscuro-browser";
        var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule!.FileName;
        var iconPath = exePath + ",0";
        var appDescription = "Chiaroscuro Browser";

        // 1. ProgID registration
        var progIdKey = $@"Software\Classes\{progId}";
        using (var progIdReg = Registry.CurrentUser.CreateSubKey(progIdKey))
        {
            progIdReg.SetValue(null, appName + " HTML Document");
            using var shell = progIdReg.CreateSubKey("shell");
            using var open = shell.CreateSubKey("open");
            using var command = open.CreateSubKey("command");
            command.SetValue(null, $"\"{exePath}\" \"%1\"");
        }

        var startMenuInternetKey = @"Software\Clients\StartMenuInternet";

        // 2. Register as browser
        var startMenuKey = $@"{startMenuInternetKey}\{appName}";
        using (var startMenuReg = Registry.CurrentUser.CreateSubKey(startMenuKey))
        {
            startMenuReg.SetValue(null, appName);
            startMenuReg.SetValue("LocalizedString", appName);
            startMenuReg.SetValue("Icon", iconPath);
        }

        // 3. Capabilities
        var capabilitiesKey = $@"{startMenuInternetKey}\{appName}\Capabilities";
        using (var capReg = Registry.CurrentUser.CreateSubKey(capabilitiesKey))
        {
            capReg.SetValue("ApplicationName", appName);
            capReg.SetValue("ApplicationDescription", appDescription);
            capReg.SetValue("ApplicationIcon", iconPath);
            using var urlAssoc = capReg.CreateSubKey("URLAssociations");
            urlAssoc.SetValue("http", progId);
            urlAssoc.SetValue("https", progId);
        }

        // 4. RegisteredApplications
        var registeredAppsKey = @"Software\RegisteredApplications";
        using (var regApps = Registry.CurrentUser.CreateSubKey(registeredAppsKey))
        {
            regApps.SetValue(appName, $@"{startMenuInternetKey}\{appName}\Capabilities");
        }
    }
}
