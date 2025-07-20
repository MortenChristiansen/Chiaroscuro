using BrowserHost.Features.Tabs;
using CefSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BrowserHost.Features.Workspaces;

public class WorkspacesFeature : Feature
{
    private readonly WorkspaceListBrowserApi _workspaceListBrowserApi;
    private WorkspacesDataDtoV2 _workspacesData;
    private readonly TabsFeature _tabsFeature;

    public WorkspacesFeature(MainWindow mainWindow) : base(mainWindow)
    {
        _workspaceListBrowserApi = new WorkspaceListBrowserApi(this);
        _tabsFeature = mainWindow.GetFeature<TabsFeature>();
        
        // Load workspaces from disk
        _workspacesData = TabStateManager.RestoreWorkspacesFromDisk();
    }

    public override void Register()
    {
        MainWindow.Chrome.JavascriptObjectRepository.Register("workspacesApi", _workspaceListBrowserApi, options: BindingOptions.DefaultBinder);
    }

    public override bool HandleOnPreviewKeyDown(KeyEventArgs e)
    {
        // Handle Alt+1, Alt+2, etc. for workspace switching
        if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
        {
            if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                var workspaceIndex = (int)(e.Key - Key.D1);
                if (workspaceIndex < _workspacesData.Workspaces.Length)
                {
                    var workspaceId = _workspacesData.Workspaces[workspaceIndex].Id;
                    ActivateWorkspace(workspaceId);
                    return true;
                }
            }
        }

        return false;
    }

    public void ActivateWorkspace(string workspaceId)
    {
        var workspace = _workspacesData.Workspaces.FirstOrDefault(w => w.Id == workspaceId);
        if (workspace == null)
        {
            Debug.WriteLine($"Workspace with ID {workspaceId} not found");
            return;
        }

        _workspacesData = _workspacesData with { ActiveWorkspaceId = workspaceId };
        
        // Save the updated workspace data
        TabStateManager.SaveWorkspacesToDisk(_workspacesData);
        
        // Notify the frontend about the workspace change
        NotifyWorkspacesChanged();
        
        // Switch the tabs feature to show this workspace's tabs
        _tabsFeature.LoadWorkspaceTabs(workspace.Tabs, workspace.LastActiveTabId);
    }

    public void CreateWorkspace(string name, string icon, string color)
    {
        var newWorkspaceId = Guid.NewGuid().ToString();
        var newWorkspace = new WorkspaceDto(newWorkspaceId, name, icon, color, [], null);
        
        var updatedWorkspaces = _workspacesData.Workspaces.Append(newWorkspace).ToArray();
        _workspacesData = new WorkspacesDataDtoV2(updatedWorkspaces, newWorkspaceId);
        
        TabStateManager.SaveWorkspacesToDisk(_workspacesData);
        NotifyWorkspacesChanged();
        
        // Switch to the new workspace
        ActivateWorkspace(newWorkspaceId);
    }

    public void UpdateWorkspace(string workspaceId, string name, string icon, string color)
    {
        var workspaceIndex = Array.FindIndex(_workspacesData.Workspaces, w => w.Id == workspaceId);
        if (workspaceIndex == -1)
        {
            Debug.WriteLine($"Workspace with ID {workspaceId} not found for update");
            return;
        }

        var currentWorkspace = _workspacesData.Workspaces[workspaceIndex];
        var updatedWorkspace = currentWorkspace with { Name = name, Icon = icon, Color = color };
        
        var updatedWorkspaces = _workspacesData.Workspaces.ToArray();
        updatedWorkspaces[workspaceIndex] = updatedWorkspace;
        
        _workspacesData = _workspacesData with { Workspaces = updatedWorkspaces };
        
        TabStateManager.SaveWorkspacesToDisk(_workspacesData);
        NotifyWorkspacesChanged();
    }

    public void DeleteWorkspace(string workspaceId)
    {
        if (_workspacesData.Workspaces.Length <= 1)
        {
            Debug.WriteLine("Cannot delete the last workspace");
            return;
        }

        var updatedWorkspaces = _workspacesData.Workspaces.Where(w => w.Id != workspaceId).ToArray();
        
        // If we're deleting the active workspace, switch to the first remaining one
        var newActiveWorkspaceId = _workspacesData.ActiveWorkspaceId == workspaceId 
            ? updatedWorkspaces[0].Id 
            : _workspacesData.ActiveWorkspaceId;
        
        _workspacesData = new WorkspacesDataDtoV2(updatedWorkspaces, newActiveWorkspaceId);
        
        TabStateManager.SaveWorkspacesToDisk(_workspacesData);
        NotifyWorkspacesChanged();
        
        // If we switched workspaces, activate the new one
        if (newActiveWorkspaceId != workspaceId)
        {
            ActivateWorkspace(newActiveWorkspaceId);
        }
    }

    public void UpdateWorkspaceTabs(string workspaceId, TabStateDtoV1[] tabs, string? lastActiveTabId)
    {
        var workspaceIndex = Array.FindIndex(_workspacesData.Workspaces, w => w.Id == workspaceId);
        if (workspaceIndex == -1)
        {
            Debug.WriteLine($"Workspace with ID {workspaceId} not found for tab update");
            return;
        }

        var currentWorkspace = _workspacesData.Workspaces[workspaceIndex];
        var updatedWorkspace = currentWorkspace with { Tabs = tabs, LastActiveTabId = lastActiveTabId };
        
        var updatedWorkspaces = _workspacesData.Workspaces.ToArray();
        updatedWorkspaces[workspaceIndex] = updatedWorkspace;
        
        _workspacesData = _workspacesData with { Workspaces = updatedWorkspaces };
        
        TabStateManager.SaveWorkspacesToDisk(_workspacesData);
    }

    public WorkspacesDataDtoV2 GetWorkspacesData() => _workspacesData;

    public string GetActiveWorkspaceId() => _workspacesData.ActiveWorkspaceId;

    private void NotifyWorkspacesChanged()
    {
        var workspaceStates = _workspacesData.Workspaces.Select(w => 
            new WorkspaceStateDto(w.Id, w.Name, w.Icon, w.Color)).ToArray();
        
        MainWindow.Chrome.GetMainFrame().EvaluateScriptAsync(
            $"if (window.angularApi && window.angularApi.workspacesChanged) {{ " +
            $"window.angularApi.workspacesChanged({System.Text.Json.JsonSerializer.Serialize(workspaceStates)}, " +
            $"'{_workspacesData.ActiveWorkspaceId}'); }}");
    }
}