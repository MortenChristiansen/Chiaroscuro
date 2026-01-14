using BrowserHost.CefInfrastructure;
using BrowserHost.Logging;
using BrowserHost.Utilities;
using System;

namespace BrowserHost.Features.ActionContext.Workspaces;

public class WorkspacesBackendApi(PubSub pubSub) : BackendApi
{
    public void ActivateWorkspace(string workspaceId) =>
        pubSub.Send(new ActivateWorkspaceCommand(workspaceId));

    public void CreateWorkspace(string name, string icon, string color) =>
        pubSub.Send(new CreateWorkspaceCommand($"{Guid.NewGuid()}", name, icon, color));

    public void UpdateWorkspace(string workspaceId, string name, string icon, string color) =>
        pubSub.Send(new UpdateWorkspaceCommand(workspaceId, name, icon, color));

    public void DeleteWorkspace(string workspaceId) =>
        pubSub.Send(new DeleteWorkspaceCommand(workspaceId));

    public void OnLoaded() =>
        Measure.Event("Workspaces frontend loaded");
}
