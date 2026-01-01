using BrowserHost.Features.ActionDialog;
using BrowserHost.Utilities;
using System.Collections.Generic;

namespace BrowserHost.CefInfrastructure;

public class BrowserProcessHandler(PubSub pubSub) : CefSharp.Handler.BrowserProcessHandler
{
    protected override bool OnAlreadyRunningAppRelaunch(IReadOnlyDictionary<string, string> commandLine, string currentDirectory)
    {
        var launchUrl = Options.GetLaunchUrl([.. commandLine.Keys]);
        if (launchUrl != null)
            pubSub.Publish(new NavigationStartedEvent(launchUrl, UseCurrentTab: false, SaveInHistory: true, ActivateTab: true));

        return true;
    }
}
