using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.PinnedTabs;

public record PinnedTabDataV1(PinnedTabDtoV1[] PinnedTabs, string? ActiveTabId);
public record PinnedTabDtoV1(string Id, string? Title, string? Favicon, string Address);

public static class PinnedTabsStateManager
{
    private static readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("pinned_tabs.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private const int _currentVersion = 1;
    private static PinnedTabDataV1 _lastSavedPinnedTabsData = new([], null);
    private static readonly Lock _lock = new();

    public static PinnedTabDataV1 SavePinnedTabs(PinnedTabDataV1 pinnedTabsData)
    {
        lock (_lock)
        {
            if (StateIsEqual(_lastSavedPinnedTabsData, pinnedTabsData))
            {
                Debug.WriteLine("Skipping pinned tabs state save - no changes detected.");
                return _lastSavedPinnedTabsData;
            }

            try
            {
                var versionedData = new PersistentData<PinnedTabDataV1>
                {
                    Version = _currentVersion,
                    Data = pinnedTabsData
                };
                File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));
                _lastSavedPinnedTabsData = pinnedTabsData;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to save pinned tabs state: {e.Message}");
            }
            return _lastSavedPinnedTabsData;
        }
    }

    public static PinnedTabDataV1 RestorePinnedTabsFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_persistedStatePath))
                {
                    var json = File.ReadAllText(_persistedStatePath);
                    var versionedData = JsonSerializer.Deserialize<PersistentData>(json);
                    if (versionedData?.Version == _currentVersion)
                    {
                        var data = JsonSerializer.Deserialize<PersistentData<PinnedTabDataV1>>(json)?.Data;
                        if (data != null)
                        {
                            _lastSavedPinnedTabsData = data;
                        }
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to restore pinned tabs state: {e.Message}");
            }
            return _lastSavedPinnedTabsData;
        }
    }

    private static bool StateIsEqual(PinnedTabDataV1 data1, PinnedTabDataV1 data2)
    {
        if (ReferenceEquals(data1, data2)) return true;
        if (data1 is null || data2 is null) return false;
        if (data1.ActiveTabId != data2.ActiveTabId) return false;
        if (data1.PinnedTabs.Length != data2.PinnedTabs.Length) return false;

        for (int i = 0; i < data1.PinnedTabs.Length; i++)
        {
            if (!data1.PinnedTabs[i].Equals(data2.PinnedTabs[i]))
                return false;
        }

        return true;
    }
}
