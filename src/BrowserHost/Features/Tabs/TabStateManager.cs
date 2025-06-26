using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrowserHost.Features.Tabs;

public static class TabStateManager
{
    private static readonly string _tabsStatePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "tabs.json"));

    public static void SaveTabsToDisk(IEnumerable<TabBrowser> tabs)
    {
        try
        {
            MainWindow.Instance?.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine("Saving tabs state to disk...");
                var states = tabs.Select(t => new TabStateDto(t.Address, t.Title, t.Favicon, t == MainWindow.Instance.CurrentTab)).ToList();
                File.WriteAllText(_tabsStatePath, JsonSerializer.Serialize(states));
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
