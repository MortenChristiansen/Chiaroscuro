using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.LocalWebApp;

public class LocalWebAppBrowserApi(BaseBrowser tabPaletteBrowser) : BrowserApi(tabPaletteBrowser)
{
    public void InitConfig(string tabId, string? directoryPath, string? startCommand, bool isRunning, bool hasErrors)
    {
        var args = $"{tabId.ToJsonString()}, {(directoryPath?.ToJsonString() ?? "null")}, {(startCommand?.ToJsonString() ?? "null")}, {isRunning.ToJsonBoolean()}, {hasErrors.ToJsonBoolean()}";
        CallClientApi("initLocalWebAppConfig", args);
    }

    public void UpdateConfig(string tabId, string directoryPath, string? startCommand)
    {
        var args = $"{tabId.ToJsonString()}, {directoryPath.ToJsonString()}, {(startCommand?.ToJsonString() ?? "null")}";
        CallClientApi("updateLocalWebAppConfig", args);
    }

    public void ClearConfig(string tabId)
    {
        var args = $"{tabId.ToJsonString()}";
        CallClientApi("clearLocalWebAppConfig", args);
    }

    public void DirectorySelected(string tabId, string directoryPath)
    {
        var args = $"{tabId.ToJsonString()}, {directoryPath.ToJsonString()}";
        CallClientApi("localWebAppDirectorySelected", args);
    }

    public void UpdateProcessStatus(string tabId, bool isRunning, bool hasErrors)
    {
        var args = $"{tabId.ToJsonString()}, {isRunning.ToJsonBoolean()}, {hasErrors.ToJsonBoolean()}";
        CallClientApi("updateLocalWebAppProcessStatus", args);
    }
}
