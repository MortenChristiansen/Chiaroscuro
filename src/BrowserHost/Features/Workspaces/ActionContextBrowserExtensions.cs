using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.Workspaces;

public record WorkspaceDto(string Id, string Name, string Color, string Icon, TabDto[] Tabs, int EphemeralTabStartIndex, string? ActiveTabId);
public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);

public static class ActionContextBrowserExtensions
{
    public static void SetWorkspaces(this ActionContextBrowser browser, WorkspaceDto[] workspaces)
    {
        browser.CallClientApi("setWorkspaces", $"{workspaces.ToJsonObject()}");
    }
}
