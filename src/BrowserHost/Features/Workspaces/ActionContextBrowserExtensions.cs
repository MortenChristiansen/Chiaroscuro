using BrowserHost.Features.ActionContext;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.Workspaces;

public record WorkspaceDescriptionDto(string Id, string Name, string Color, string Icon);
public record WorkspaceDto(string Id, string Name, string Color, string Icon, TabDto[] Tabs, int EphemeralTabStartIndex, string? ActiveTabId, FolderDto[] Folders) : WorkspaceDescriptionDto(Id, Name, Color, Icon);
public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);
public record FolderDto(string Id, string Name, int StartIndex, int EndIndex);

public static class ActionContextBrowserExtensions
{
    public static void SetWorkspaces(this ActionContextBrowser browser, WorkspaceDto[] workspaces)
    {
        browser.CallClientApi("setWorkspaces", $"{workspaces.ToJsonObject()}");
    }

    public static void WorkspacesChanged(this ActionContextBrowser browser, WorkspaceDescriptionDto[] workspaces)
    {
        browser.CallClientApi("workspacesChanged", $"{workspaces.ToJsonObject()}");
    }

    public static void WorkspaceActivated(this ActionContextBrowser browser, string activeWorkspaceId)
    {
        browser.CallClientApi("workspaceActivated", $"{activeWorkspaceId.ToJsonString()}");
    }
}
