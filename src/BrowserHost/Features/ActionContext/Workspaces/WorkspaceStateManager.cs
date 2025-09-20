using BrowserHost.Logging;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.ActionContext.Workspaces;

public record WorkspacesDataDtoV1(WorkspaceDtoV1[] Workspaces);
public record WorkspaceDtoV1(string WorkspaceId, string Name, string Color, string Icon, WorkspaceTabStateDtoV1[] Tabs, int EphemeralTabStartIndex)
{
    public FolderDtoV1[] Folders { get; init; } = [];
}
public record WorkspaceTabStateDtoV1(string TabId, string Address, string? Title, string? Favicon, bool IsActive, DateTimeOffset Created);
public record FolderDtoV1(string Id, string Name, int StartIndex, int EndIndex);

public static class WorkspaceStateManager
{
    private static readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("workspaces.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private const int _currentVersion = 1;
    private const int _ephemeralTabExpirationHours = 16;
    private static readonly WorkspaceDtoV1 _defaultWorkspace = new($"{Guid.NewGuid()}", "Browse", "#202634", "🌐", [], 0);
    private static WorkspacesDataDtoV1? _lastSavedWorkspaceData;
    private static readonly Lock _lock = new();

    public static WorkspaceDtoV1[] SaveWorkspaceTabs(string workspaceId, IEnumerable<WorkspaceTabStateDtoV1> tabs, int ephemeralTabStartIndex, IEnumerable<FolderDtoV1> folders)
    {
        lock (_lock)
        {
            var workspace = _lastSavedWorkspaceData?.Workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspaceId) ?? _defaultWorkspace;
            var newTabsData = workspace with
            {
                Tabs = [.. tabs],
                EphemeralTabStartIndex = ephemeralTabStartIndex,
                Folders = [.. folders]
            };

            SaveWorkspaceIfChanged(workspaceId, workspace, newTabsData);
        }
        return _lastSavedWorkspaceData!.Workspaces;
    }

    private static void SaveWorkspaceIfChanged(string workspaceId, WorkspaceDtoV1 cachedWorkspace, WorkspaceDtoV1 updatedWorkspace)
    {
        // Check if the new data is the same as what we last saved
        if (_lastSavedWorkspaceData != null && StateIsEqual(cachedWorkspace, updatedWorkspace))
        {
            Debug.WriteLine("Skipping workspace state save - no changes detected.");
            return;
        }

        Debug.WriteLine("Saving workspace state to disk...");
        var existingData = _lastSavedWorkspaceData ?? new WorkspacesDataDtoV1([]);
        var existingDataWithUpdatedWorkspace = existingData.Workspaces
            .Where(ws => ws.WorkspaceId != workspaceId)
            .Append(updatedWorkspace)
            .OrderBy(ws => ws.Name)
            .ToArray();

        SaveWorkspaces(existingDataWithUpdatedWorkspace);
    }

    private static void SaveWorkspaces(WorkspaceDtoV1[] updatedWorkspaces)
    {
        try
        {
            var newWorkspacesData = new WorkspacesDataDtoV1(updatedWorkspaces);
            var versionedData = new PersistentData<WorkspacesDataDtoV1>
            {
                Version = _currentVersion,
                Data = newWorkspacesData
            };
            File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));

            // Update the cache after successful save
            _lastSavedWorkspaceData = newWorkspacesData;
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to save workspace state: {e.Message}");
        }
    }

    public static WorkspaceDtoV1[] RestoreWorkspacesFromDisk()
    {
        using (Measure.Operation("Restoring workspaces from disk"))
        {
            lock (_lock)
            {
                WorkspacesDataDtoV1? result = null;

                try
                {
                    if (File.Exists(_persistedStatePath))
                    {
                        var json = File.ReadAllText(_persistedStatePath);

                        try
                        {
                            var versionedData = JsonSerializer.Deserialize<PersistentData>(json);
                            if (versionedData?.Version == _currentVersion)
                            {
                                var rawData = JsonSerializer.Deserialize<PersistentData<WorkspacesDataDtoV1>>(json)?.Data ?? new([_defaultWorkspace]);
                                result = FilterExpiredEphemeralTabs(rawData);
                            }
                        }
                        catch (Exception e) when (!Debugger.IsAttached)
                        {
                            Debug.WriteLine($"Failed to restore tabs state: {e.Message}");
                        }
                    }
                }
                catch (Exception e2) when (!Debugger.IsAttached)
                {
                    Debug.WriteLine($"Failed to restore tabs state: {e2.Message}");
                }

                // Update the cache with the restored data (after filtering expired tabs)
                _lastSavedWorkspaceData = result ?? new([_defaultWorkspace]);

                // We ensure that there is always at least one workspace available
                if (_lastSavedWorkspaceData.Workspaces.Length == 0)
                    _lastSavedWorkspaceData = new([_defaultWorkspace]);

                return _lastSavedWorkspaceData.Workspaces;
            }
        }
    }

    private static WorkspacesDataDtoV1 FilterExpiredEphemeralTabs(WorkspacesDataDtoV1 workspaceData)
    {
        var now = DateTimeOffset.UtcNow;

        WorkspaceDtoV1 FilterExpiredTabs(WorkspaceDtoV1 tabsData)
        {
            var ephemeralTabStartIndex = Math.Min(tabsData.EphemeralTabStartIndex, tabsData.Tabs.Length);
            var persistentTabs = ephemeralTabStartIndex > 0 ? tabsData.Tabs[..ephemeralTabStartIndex] : [];
            var ephemeralTabs = ephemeralTabStartIndex < tabsData.Tabs.Length ? tabsData.Tabs[ephemeralTabStartIndex..] : [];
            var expiredTabs = ephemeralTabs.Where(t => (now - t.Created).TotalHours >= _ephemeralTabExpirationHours).ToArray();
            PubSub.Publish(new EphemeralTabsExpiredEvent([.. expiredTabs.Select(t => t.TabId)]));
            ephemeralTabs = [.. ephemeralTabs.Except(expiredTabs)];
            return tabsData with { Tabs = [.. persistentTabs, .. ephemeralTabs], EphemeralTabStartIndex = ephemeralTabStartIndex };
        }

        return workspaceData with { Workspaces = [.. workspaceData.Workspaces.Select(FilterExpiredTabs)] };
    }

    private static bool StateIsEqual(WorkspaceDtoV1 data1, WorkspaceDtoV1 data2)
    {
        if (data1.EphemeralTabStartIndex != data2.EphemeralTabStartIndex) return false;
        if (data1.Tabs.Length != data2.Tabs.Length) return false;
        if (data1.Name != data2.Name) return false;
        if (data1.Icon != data2.Icon) return false;
        if (data1.Color != data2.Color) return false;
        if (data1.Folders.Length != data2.Folders.Length) return false;

        for (int i = 0; i < data1.Tabs.Length; i++)
        {
            if (!data1.Tabs[i].Equals(data2.Tabs[i]))
                return false;
        }

        for (int i = 0; i < data1.Folders.Length; i++)
        {
            if (!data1.Folders[i].Equals(data2.Folders[i]))
                return false;
        }

        return true;
    }

    public static WorkspaceDtoV1[] CreateWorkspace(WorkspaceDtoV1 workspace)
    {
        lock (_lock)
        {
            if (_lastSavedWorkspaceData == null)
                throw new InvalidOperationException("No workspaces loaded");

            SaveWorkspaces([.. _lastSavedWorkspaceData.Workspaces.Append(workspace).OrderBy(ws => ws.Name)]);
        }
        return _lastSavedWorkspaceData.Workspaces;
    }

    public static WorkspaceDtoV1[] UpdateWorkspace(WorkspaceDtoV1 workspace)
    {
        lock (_lock)
        {
            var cached = _lastSavedWorkspaceData?.Workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspace.WorkspaceId) ?? throw new ArgumentException("Workspace does not exist");
            SaveWorkspaceIfChanged(workspace.WorkspaceId, cached, workspace);
        }
        return _lastSavedWorkspaceData.Workspaces;
    }

    public static WorkspaceDtoV1[] DeleteWorkspace(string workspaceId)
    {
        lock (_lock)
        {
            if (_lastSavedWorkspaceData == null)
                throw new InvalidOperationException("No workspaces loaded");

            SaveWorkspaces([.. _lastSavedWorkspaceData.Workspaces.Where(ws => ws.WorkspaceId != workspaceId)]);
        }
        return _lastSavedWorkspaceData.Workspaces;
    }
}
