using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public readonly record struct TabCustomizationDataV1(string? CustomTitle);

public static class TabCustomizationStateManager
{
    private const int _currentVersion = 1;
    private static readonly Lock _lock = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    // Cache customizations per tab on-demand only
    private static readonly Dictionary<string, TabCustomizationDataV1> _cachedPerTab = [];

    private static string RootFolder => Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "tab-customization");
    private static string GetTabFolder(string tabId) => Path.Combine(RootFolder, Sanitize(tabId));
    private static string GetCustomizationFilePath(string tabId) => Path.Combine(GetTabFolder(tabId), "customization.json");

    // Keep tab folder names simple/safe for filesystem
    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return value;
    }

    public static TabCustomizationDataV1 GetCustomization(string tabId)
    {
        lock (_lock)
        {
            if (_cachedPerTab.TryGetValue(tabId, out var cached))
                return cached;

            var file = GetCustomizationFilePath(tabId);
            var data = new TabCustomizationDataV1(null);

            try
            {
                if (File.Exists(file))
                {
                    var json = File.ReadAllText(file);
                    var versioned = JsonSerializer.Deserialize<PersistentData>(json);
                    if (versioned?.Version == _currentVersion)
                    {
                        var payload = JsonSerializer.Deserialize<PersistentData<TabCustomizationDataV1>>(json)?.Data;
                        if (payload.HasValue)
                            data = payload.Value;
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to read tab customization for '{tabId}': {e.Message}");
            }

            _cachedPerTab[tabId] = data;
            return data;
        }
    }

    public static TabCustomizationDataV1 SaveCustomization(string tabId, TabCustomizationDataV1 data)
    {
        lock (_lock)
        {
            if (_cachedPerTab.TryGetValue(tabId, out var existing) && existing.Equals(data))
            {
                Debug.WriteLine("Skipping tab customization save - no changes detected.");
                return existing;
            }

            var folder = GetTabFolder(tabId);
            var file = GetCustomizationFilePath(tabId);

            try
            {
                Directory.CreateDirectory(folder);

                var versioned = new PersistentData<TabCustomizationDataV1>
                {
                    Version = _currentVersion,
                    Data = data
                };

                File.WriteAllText(file, JsonSerializer.Serialize(versioned, _jsonSerializerOptions));
                _cachedPerTab[tabId] = data;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to save tab customization for '{tabId}': {e.Message}");
            }

            return _cachedPerTab[tabId];
        }
    }

    public static void DeleteCustomization(string tabId)
    {
        lock (_lock)
        {
            _cachedPerTab.Remove(tabId);

            var folder = GetTabFolder(tabId);
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to delete tab customization for '{tabId}': {e.Message}");
            }
        }
    }
}
