using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System.Collections.Generic;

namespace BrowserHost.CefInfrastructure;

public class BrowserProcessHandler : CefSharp.Handler.BrowserProcessHandler
{
    protected override bool OnAlreadyRunningAppRelaunch(IReadOnlyDictionary<string, string> commandLine, string currentDirectory)
    {
        var launchUrl = Options.GetLaunchUrl([.. commandLine.Keys]);
        if (launchUrl != null)
            PubSub.Publish(new NavigationStartedEvent(launchUrl, UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));

        return true;
    }
}
