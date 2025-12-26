using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.ActionContext.Workspaces;

public record WorkspaceDescriptionDto(string Id, string Name, string Color, string Icon);
public record WorkspaceDto(string Id, string Name, string Color, string Icon, TabDto[] Tabs, int EphemeralTabStartIndex, string? ActiveTabId, FolderDto[] Folders) : WorkspaceDescriptionDto(Id, Name, Color, Icon);
public record TabDto(string Id, string? Title, string? Favicon, DateTimeOffset Created);
public record FolderDto(string Id, string Name, int StartIndex, int EndIndex);

public class WorkspacesBrowserApi(BaseBrowser actionContextBrowser) : BrowserApi(actionContextBrowser)
{
    public void SetWorkspaces(WorkspaceDto[] workspaces) =>
        CallClientApi("setWorkspaces", $"{workspaces.ToJsonObject()}");

    public void WorkspacesChanged(WorkspaceDescriptionDto[] workspaces) =>
        CallClientApi("workspacesChanged", $"{workspaces.ToJsonObject()}");

    public void WorkspaceActivated(string activeWorkspaceId) =>
        CallClientApi("workspaceActivated", $"{activeWorkspaceId.ToJsonString()}");
}
