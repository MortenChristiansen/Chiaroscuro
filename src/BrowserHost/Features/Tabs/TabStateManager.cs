using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BrowserHost.Features.Tabs;

public static class TabStateManager
{
    private static readonly string _tabsStatePath = AppDataPathManager.GetAppDataFilePath("tabs.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    public static void SaveTabsToDisk(IEnumerable<TabStateDto> tabs)
    {
        try
        {
            MainWindow.Instance?.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine("Saving tabs state to disk...");
                File.WriteAllText(_tabsStatePath, JsonSerializer.Serialize(tabs, _jsonSerializerOptions));
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to save tabs state: {e.Message}");
        }
    }

    public static List<TabStateDto> RestoreTabsFromDisk()
    {
        try
        {
            if (File.Exists(_tabsStatePath))
            {
                var json = File.ReadAllText(_tabsStatePath);
                return JsonSerializer.Deserialize<List<TabStateDto>>(json) ?? [];
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to restore tabs state: {e.Message}");
        }

        return [];
    }
}
