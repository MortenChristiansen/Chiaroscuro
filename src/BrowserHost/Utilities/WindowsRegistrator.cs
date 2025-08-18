using Microsoft.Win32;
using NuGet.Versioning;
using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace BrowserHost.Utilities;

public static class WindowsRegistrator
{
    [SupportedOSPlatform("windows")]
    public static void RegisterApplication(SemanticVersion version)
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
