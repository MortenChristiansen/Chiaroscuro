using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrowserHost.Features.Tabs;

public record TabsDataDtoV1(TabStateDtoV1[] Tabs, int EphemeralTabStartIndex);
public record TabStateDtoV1(string Address, string? Title, string? Favicon, bool IsActive, DateTimeOffset Created);

public static class TabStateManager
{
    private static readonly string _tabsStatePath = AppDataPathManager.GetAppDataFilePath("tabs.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private const int _currentVersion = 2;
    private const int _ephemeralTabExpirationHours = 16;
    private static readonly TabsDataDtoV1 _emptyTabs = new([], 0);
    private static TabsDataDtoV1? _lastSavedTabsData;
    private static WorkspacesDataDtoV2? _lastSavedWorkspacesData;

    public static void SaveTabsToDisk(IEnumerable<TabStateDtoV1> tabs, int ephemeralTabStartIndex)
    {
        var newTabsData = new TabsDataDtoV1([.. tabs], ephemeralTabStartIndex);

        // Check if the new data is the same as what we last saved
        if (_lastSavedTabsData != null && TabsDataEqual(_lastSavedTabsData, newTabsData))
        {
            Debug.WriteLine("Skipping tabs state save - no changes detected.");
            return;
        }

        MainWindow.Instance?.Dispatcher.Invoke(() =>
        {
            try
            {
                Debug.WriteLine("Saving tabs state to disk...");
                var versionedData = new PersistentData<TabsDataDtoV1>
                {
                    Version = 1, // Keep version 1 for backward compatibility when only saving tabs
                    Data = newTabsData
                };
                File.WriteAllText(_tabsStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));

                // Update the cache after successful save
                _lastSavedTabsData = newTabsData;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to save tabs state: {e.Message}");
            }
        });
    }

    public static void SaveWorkspacesToDisk(WorkspacesDataDtoV2 workspacesData)
    {
        // Check if the new data is the same as what we last saved
        if (_lastSavedWorkspacesData != null && WorkspacesDataEqual(_lastSavedWorkspacesData, workspacesData))
        {
            Debug.WriteLine("Skipping workspaces state save - no changes detected.");
            return;
        }

        MainWindow.Instance?.Dispatcher.Invoke(() =>
        {
            try
            {
                Debug.WriteLine("Saving workspaces state to disk...");
                var versionedData = new PersistentData<WorkspacesDataDtoV2>
                {
                    Version = _currentVersion,
                    Data = workspacesData
                };
                File.WriteAllText(_tabsStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));

                // Update the cache after successful save
                _lastSavedWorkspacesData = workspacesData;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to save workspaces state: {e.Message}");
            }
        });
    }

    public static TabsDataDtoV1 RestoreTabsFromDisk()
    {
        TabsDataDtoV1 result = _emptyTabs;

        try
        {
            if (File.Exists(_tabsStatePath))
            {
                var json = File.ReadAllText(_tabsStatePath);

                try
                {
                    var versionedData = JsonSerializer.Deserialize<PersistentData>(json);
                    if (versionedData?.Version == 1)
                    {
                        var rawData = JsonSerializer.Deserialize<PersistentData<TabsDataDtoV1>>(json)?.Data ?? _emptyTabs;
                        result = FilterExpiredEphemeralTabs(rawData);
                    }
                    else if (versionedData?.Version == _currentVersion)
                    {
                        // Version 2 data exists, but this method is for legacy tab restoration
                        // Return empty to indicate we should use workspace restoration instead
                        result = _emptyTabs;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to restore tabs state: {e.Message}");
                    result = _emptyTabs;
                }
            }
        }
        catch (Exception e2)
        {
            Debug.WriteLine($"Failed to restore tabs state: {e2.Message}");
        }

        // Update the cache with the restored data (after filtering expired tabs)
        _lastSavedTabsData = result;
        return result;
    }

    public static WorkspacesDataDtoV2 RestoreWorkspacesFromDisk()
    {
        WorkspacesDataDtoV2? result = null;

        try
        {
            if (File.Exists(_tabsStatePath))
            {
                var json = File.ReadAllText(_tabsStatePath);

                try
                {
                    var versionedData = JsonSerializer.Deserialize<PersistentData>(json);
                    if (versionedData?.Version == _currentVersion)
                    {
                        var rawData = JsonSerializer.Deserialize<PersistentData<WorkspacesDataDtoV2>>(json)?.Data;
                        if (rawData != null)
                        {
                            // Filter expired ephemeral tabs from all workspaces
                            var filteredWorkspaces = rawData.Workspaces.Select(w => new WorkspaceDto(
                                w.Id,
                                w.Name,
                                w.Icon,
                                w.Color,
                                FilterExpiredEphemeralTabs(new TabsDataDtoV1(w.Tabs, GetEphemeralStartIndex(w.Tabs))).Tabs,
                                w.LastActiveTabId
                            )).ToArray();

                            result = new WorkspacesDataDtoV2(filteredWorkspaces, rawData.ActiveWorkspaceId);
                        }
                    }
                    else if (versionedData?.Version == 1)
                    {
                        // Migrate from version 1 to version 2
                        var v1Data = JsonSerializer.Deserialize<PersistentData<TabsDataDtoV1>>(json)?.Data ?? _emptyTabs;
                        result = MigrateV1ToV2(v1Data);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to restore workspaces state: {e.Message}");
                }
            }
        }
        catch (Exception e2)
        {
            Debug.WriteLine($"Failed to restore workspaces state: {e2.Message}");
        }

        // If no valid data found, create default workspace
        if (result == null)
        {
            var defaultWorkspaceId = Guid.NewGuid().ToString();
            var defaultWorkspace = new WorkspaceDto(
                defaultWorkspaceId,
                "Browsing",
                "🌐", // Default icon
                "#2563eb", // Default blue color
                [],
                null
            );
            result = new WorkspacesDataDtoV2([defaultWorkspace], defaultWorkspaceId);
        }

        // Update the cache with the restored data
        _lastSavedWorkspacesData = result;
        return result;
    }

    private static WorkspacesDataDtoV2 MigrateV1ToV2(TabsDataDtoV1 v1Data)
    {
        var filteredTabs = FilterExpiredEphemeralTabs(v1Data);
        var workspaceId = Guid.NewGuid().ToString();
        var activeTabId = filteredTabs.Tabs.FirstOrDefault(t => t.IsActive)?.Address;
        
        var defaultWorkspace = new WorkspaceDto(
            workspaceId,
            "Browsing",
            "🌐", // Default icon
            "#2563eb", // Default blue color
            filteredTabs.Tabs,
            activeTabId
        );

        return new WorkspacesDataDtoV2([defaultWorkspace], workspaceId);
    }

    private static int GetEphemeralStartIndex(TabStateDtoV1[] tabs)
    {
        // For migrated or new workspaces, assume all tabs are persistent initially
        // This can be adjusted based on business logic
        return tabs.Length;
    }

    private static TabsDataDtoV1 FilterExpiredEphemeralTabs(TabsDataDtoV1 tabsData)
    {
        var now = DateTimeOffset.UtcNow;
        var persistentTabs = tabsData.EphemeralTabStartIndex > 0 ? tabsData.Tabs[..tabsData.EphemeralTabStartIndex] : [];
        var ephemeralTabs = tabsData.EphemeralTabStartIndex < tabsData.Tabs.Length ? tabsData.Tabs[tabsData.EphemeralTabStartIndex..] : [];
        ephemeralTabs = [.. ephemeralTabs.Where(t => (now - t.Created).TotalHours < _ephemeralTabExpirationHours)];
        return new TabsDataDtoV1([.. persistentTabs, .. ephemeralTabs], tabsData.EphemeralTabStartIndex);
    }

    private static bool TabsDataEqual(TabsDataDtoV1 data1, TabsDataDtoV1 data2)
    {
        if (data1.EphemeralTabStartIndex != data2.EphemeralTabStartIndex)
            return false;

        if (data1.Tabs.Length != data2.Tabs.Length)
            return false;

        for (int i = 0; i < data1.Tabs.Length; i++)
        {
            if (!data1.Tabs[i].Equals(data2.Tabs[i]))
                return false;
        }

        return true;
    }

    private static bool WorkspacesDataEqual(WorkspacesDataDtoV2 data1, WorkspacesDataDtoV2 data2)
    {
        if (data1.ActiveWorkspaceId != data2.ActiveWorkspaceId)
            return false;

        if (data1.Workspaces.Length != data2.Workspaces.Length)
            return false;

        for (int i = 0; i < data1.Workspaces.Length; i++)
        {
            var w1 = data1.Workspaces[i];
            var w2 = data2.Workspaces[i];

            if (w1.Id != w2.Id || w1.Name != w2.Name || w1.Icon != w2.Icon || 
                w1.Color != w2.Color || w1.LastActiveTabId != w2.LastActiveTabId)
                return false;

            if (w1.Tabs.Length != w2.Tabs.Length)
                return false;

            for (int j = 0; j < w1.Tabs.Length; j++)
            {
                if (!w1.Tabs[j].Equals(w2.Tabs[j]))
                    return false;
            }
        }

        return true;
    }
}
