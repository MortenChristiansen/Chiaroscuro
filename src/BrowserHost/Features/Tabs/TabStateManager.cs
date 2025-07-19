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
    private const int _currentVersion = 1;
    private const int _ephemeralTabExpirationHours = 16;
    private static readonly TabsDataDtoV1 _emptyTabs = new([], 0);

    public static void SaveTabsToDisk(IEnumerable<TabStateDtoV1> tabs, int ephemeralTabStartIndex)
    {
        try
        {
            MainWindow.Instance?.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine("Saving tabs state to disk...");
                var versionedData = new PersistentData<TabsDataDtoV1>
                {
                    Version = _currentVersion,
                    Data = new TabsDataDtoV1([.. tabs], ephemeralTabStartIndex)
                };
                File.WriteAllText(_tabsStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to save tabs state: {e.Message}");
        }
    }

    public static TabsDataDtoV1 RestoreTabsFromDisk()
    {
        try
        {
            if (File.Exists(_tabsStatePath))
            {
                var json = File.ReadAllText(_tabsStatePath);

                try
                {
                    var versionedData = JsonSerializer.Deserialize<PersistentData>(json);
                    if (versionedData?.Version == _currentVersion)
                        return FilterExpiredEphemeralTabs(JsonSerializer.Deserialize<PersistentData<TabsDataDtoV1>>(json)?.Data ?? _emptyTabs);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to restore tabs state: {e.Message}");
                    return _emptyTabs;
                }
            }
        }
        catch (Exception e2)
        {
            Debug.WriteLine($"Failed to restore tabs state: {e2.Message}");
        }

        return _emptyTabs;
    }

    private static TabsDataDtoV1 FilterExpiredEphemeralTabs(TabsDataDtoV1 tabsData)
    {
        var now = DateTimeOffset.UtcNow;
        var persistentTabs = tabsData.EphemeralTabStartIndex > 0 ? tabsData.Tabs[..tabsData.EphemeralTabStartIndex] : [];
        var ephemeralTabs = tabsData.EphemeralTabStartIndex < tabsData.Tabs.Length ? tabsData.Tabs[tabsData.EphemeralTabStartIndex..] : [];
        ephemeralTabs = [.. ephemeralTabs.Where(t => (now - t.Created).TotalHours < _ephemeralTabExpirationHours)];
        return new TabsDataDtoV1([.. persistentTabs, .. ephemeralTabs], tabsData.EphemeralTabStartIndex);
    }
}
