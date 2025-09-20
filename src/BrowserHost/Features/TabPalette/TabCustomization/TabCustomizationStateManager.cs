using BrowserHost.Logging;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record TabCustomizationDataV1(string TabId, string? CustomTitle, bool? DisableFixedAddress);

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
        // We prepopulate the cache when loading all customizations, so if we miss here it means
        // that there is no customization saved for this tab.

        lock (_lock)
        {
            if (_cachedPerTab.TryGetValue(tabId, out var cached))
                return cached;

            return CreateDefaultCustomization(tabId);
        }
    }

    public static IReadOnlyCollection<TabCustomizationDataV1> GetAllCustomizations()
    {
        // This is not a very efficient implementation, but this is only called once.
        // We may want to optimize this later if needed.

        using (Measure.Operation("Restoring tab customizations from disk"))
        {
            lock (_lock)
            {
                var results = new List<TabCustomizationDataV1>();

                try
                {
                    if (Directory.Exists(RootFolder))
                    {
                        foreach (var dir in Directory.EnumerateDirectories(RootFolder))
                        {
                            var file = Path.Combine(dir, "customization.json");
                            if (!File.Exists(file))
                                continue;
                            try
                            {
                                var json = File.ReadAllText(file);
                                var versioned = JsonSerializer.Deserialize<PersistentData>(json);
                                if (versioned?.Version == _currentVersion)
                                {
                                    var tabId = Path.GetFileName(dir);
                                    var data = JsonSerializer.Deserialize<PersistentData<TabCustomizationDataV1>>(json)?.Data ?? CreateDefaultCustomization(tabId);
                                    _cachedPerTab[data.TabId] = data;
                                }
                            }
                            catch (Exception e) when (!Debugger.IsAttached)
                            {
                                Debug.WriteLine($"Failed to read tab customization file '{file}': {e.Message}");
                            }
                        }
                    }
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    Debug.WriteLine($"Failed to enumerate tab customizations: {e.Message}");
                }

                results.AddRange(_cachedPerTab.Values);
                return results.AsReadOnly();
            }
        }
    }

    private static TabCustomizationDataV1 CreateDefaultCustomization(string tabId) => new(tabId, null, false);

    public static TabCustomizationDataV1? SaveCustomization(string tabId, Func<TabCustomizationDataV1, TabCustomizationDataV1> updateData)
    {
        lock (_lock)
        {
            TabCustomizationDataV1 data;
            if (_cachedPerTab.TryGetValue(tabId, out var existing))
            {
                data = updateData(existing);

                if (existing == data)
                {
                    Debug.WriteLine("Skipping tab customization save - no changes detected.");
                    return existing;
                }
            }
            else
            {
                data = updateData(CreateDefaultCustomization(tabId));
            }

            if (data == CreateDefaultCustomization(tabId))
            {
                // No customization to save
                DeleteCustomization(data.TabId);
                return null;
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
                return CreateDefaultCustomization(tabId);
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
