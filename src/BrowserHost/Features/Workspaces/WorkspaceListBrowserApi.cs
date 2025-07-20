using BrowserHost.Features.Tabs;
using System.Collections.Generic;
using System.Linq;

namespace BrowserHost.Features.Workspaces;

public interface WorkspaceListApi
{
    void ActivateWorkspace(string workspaceId);
    void CreateWorkspace(string name, string icon, string color);
    void UpdateWorkspace(string workspaceId, string name, string icon, string color);
    void DeleteWorkspace(string workspaceId);
    void WorkspacesChanged(WorkspaceStateDto[] workspaces, string activeWorkspaceId);
}

public class WorkspaceListBrowserApi
{
    private readonly WorkspacesFeature _workspacesFeature;

    public WorkspaceListBrowserApi(WorkspacesFeature workspacesFeature)
    {
        _workspacesFeature = workspacesFeature;
    }

    public void ActivateWorkspace(string workspaceId)
    {
        _workspacesFeature.ActivateWorkspace(workspaceId);
    }

    public void CreateWorkspace(string name, string icon, string color)
    {
        _workspacesFeature.CreateWorkspace(name, icon, color);
    }

    public void UpdateWorkspace(string workspaceId, string name, string icon, string color)
    {
        _workspacesFeature.UpdateWorkspace(workspaceId, name, icon, color);
    }

    public void DeleteWorkspace(string workspaceId)
    {
        _workspacesFeature.DeleteWorkspace(workspaceId);
    }
}