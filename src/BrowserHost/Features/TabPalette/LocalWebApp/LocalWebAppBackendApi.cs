using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.LocalWebApp;

public class LocalWebAppBackendApi(PubSub pubSub) : BackendApi
{
    public void SaveConfig(string tabId, string directoryPath, string? startCommand)
    {
        pubSub.Send(new SaveLocalWebAppConfigCommand(tabId, directoryPath, startCommand));
    }

    public void DeleteConfig(string tabId)
    {
        pubSub.Send(new DeleteLocalWebAppConfigCommand(tabId));
    }

    public void BrowseDirectory(string tabId)
    {
        pubSub.Send(new BrowseLocalWebAppDirectoryCommand(tabId));
    }
}
