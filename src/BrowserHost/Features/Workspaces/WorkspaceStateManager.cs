using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.Workspaces;

public record WorkspacesDataDtoV1(WorkspaceDtoV1[] Workspaces);
public record WorkspaceDtoV1(string WorkspaceId, string Name, string Color, WorkspaceTabStateDtoV1[] Tabs, int EphemeralTabStartIndex);
public record WorkspaceTabStateDtoV1(string TabId, string Address, string? Title, string? Favicon, bool IsActive, DateTimeOffset Created);

public static class WorkspaceStateManager
{
    private static readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("workspaces.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private const int _currentVersion = 1;
    private const int _ephemeralTabExpirationHours = 16;
    private static readonly WorkspaceDtoV1 _defaultWorkspace = new($"{Guid.NewGuid()}", "Browse", "#202634", [], 0);
    private static WorkspacesDataDtoV1? _lastSavedWorkspaceData;
    private static readonly Lock _lock = new();

    public static void SaveWorkspacesToDisk(string workspaceId, IEnumerable<WorkspaceTabStateDtoV1> tabs, int ephemeralTabStartIndex)
    {
        lock (_lock)
        {
            var workspace = _lastSavedWorkspaceData?.Workspaces.FirstOrDefault(ws => ws.WorkspaceId == workspaceId) ?? _defaultWorkspace;
            var newTabsData = workspace with { Tabs = [.. tabs], EphemeralTabStartIndex = ephemeralTabStartIndex };

            // Check if the new data is the same as what we last saved
            if (_lastSavedWorkspaceData != null && StateIsEqual(workspace, newTabsData))
            {
                Debug.WriteLine("Skipping workspace state save - no changes detected.");
                return;
            }

            try
            {
                Debug.WriteLine("Saving workspace state to disk...");
                var existingData = _lastSavedWorkspaceData ?? new WorkspacesDataDtoV1([]);
                var existingDataWithUpdatedWorkspace = existingData.Workspaces
                    .Where(ws => ws.WorkspaceId != workspaceId)
                    .Append(newTabsData)
                    .OrderBy(ws => ws.Name)
                    .ToArray();
                var newWorkspacesData = new WorkspacesDataDtoV1(existingDataWithUpdatedWorkspace);
                var versionedData = new PersistentData<WorkspacesDataDtoV1>
                {
                    Version = _currentVersion,
                    Data = newWorkspacesData
                };
                File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));

                // Update the cache after successful save
                _lastSavedWorkspaceData = newWorkspacesData;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to save workspace state: {e.Message}");
            }
        }
    }

    public static WorkspaceDtoV1[] RestoreWorkspacesFromDisk()
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
                    catch (Exception e)
                    {
                        Debug.WriteLine($"Failed to restore tabs state: {e.Message}");
                    }
                }
            }
            catch (Exception e2)
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

    private static WorkspacesDataDtoV1 FilterExpiredEphemeralTabs(WorkspacesDataDtoV1 workspaceData)
    {
        var now = DateTimeOffset.UtcNow;

        WorkspaceDtoV1 FilterExpiredTabs(WorkspaceDtoV1 tabsData)
        {
            var persistentTabs = tabsData.EphemeralTabStartIndex > 0 ? tabsData.Tabs[..tabsData.EphemeralTabStartIndex] : [];
            var ephemeralTabs = tabsData.EphemeralTabStartIndex < tabsData.Tabs.Length ? tabsData.Tabs[tabsData.EphemeralTabStartIndex..] : [];
            ephemeralTabs = [.. ephemeralTabs.Where(t => (now - t.Created).TotalHours < _ephemeralTabExpirationHours)];
            return tabsData with { Tabs = [.. persistentTabs, .. ephemeralTabs], EphemeralTabStartIndex = tabsData.EphemeralTabStartIndex };
        }

        return workspaceData with { Workspaces = [.. workspaceData.Workspaces.Select(FilterExpiredTabs)] };
    }

    private static bool StateIsEqual(WorkspaceDtoV1 data1, WorkspaceDtoV1 data2)
    {
        if (data1.EphemeralTabStartIndex != data2.EphemeralTabStartIndex)
            return false;

        if (data1.Tabs.Length != data2.Tabs.Length)
            return false;

        if (data1.Name != data2.Name) return false;

        if (data1.Color != data2.Color) return false;

        for (int i = 0; i < data1.Tabs.Length; i++)
        {
            if (!data1.Tabs[i].Equals(data2.Tabs[i]))
                return false;
        }

        return true;
    }
}
