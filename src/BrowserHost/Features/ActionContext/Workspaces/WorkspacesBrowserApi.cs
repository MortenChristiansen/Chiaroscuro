using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.ActionContext.Workspaces;

public record WorkspaceActivatedEvent(string WorkspaceId);
public record WorkspaceCreatedEvent(string WorkspaceId, string Name, string Icon, string Color);
public record WorkspaceUpdatedEvent(string WorkspaceId, string Name, string Icon, string Color);
public record WorkspaceDeletedEvent(string WorkspaceId);

public class WorkspacesBrowserApi() : BrowserApi
{
    public void ActivateWorkspace(string workspaceId) =>
        PubSub.Publish(new WorkspaceActivatedEvent(workspaceId));

    public void CreateWorkspace(string name, string icon, string color) =>
        PubSub.Publish(new WorkspaceCreatedEvent($"{Guid.NewGuid()}", name, icon, color));

    public void UpdateWorkspace(string workspaceId, string name, string icon, string color) =>
        PubSub.Publish(new WorkspaceUpdatedEvent(workspaceId, name, icon, color));

    public void DeleteWorkspace(string workspaceId) =>
        PubSub.Publish(new WorkspaceDeletedEvent(workspaceId));
}
