using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.AppLayout;

public record AppLayoutDataV1(double ActionContextWidth, double TabPaletteWidth);

public static class AppLayoutStateManager
{
    private static readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("appLayout.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private const int _currentVersion = 1;
    private static AppLayoutDataV1? _lastSavedLayout;
    private static readonly Lock _lock = new();

    private static AppLayoutDataV1 Default => new(ActionContextWidth: 300, TabPaletteWidth: 350);

    public static AppLayoutDataV1 RestoreAppLayoutFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_persistedStatePath))
                {
                    var json = File.ReadAllText(_persistedStatePath);
                    var versioned = JsonSerializer.Deserialize<PersistentData>(json);
                    if (versioned?.Version == _currentVersion)
                    {
                        var parsed = JsonSerializer.Deserialize<PersistentData<AppLayoutDataV1>>(json);
                        if (parsed?.Data is not null)
                            _lastSavedLayout = parsed.Data;
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to restore app layout: {e.Message}");
            }

            return _lastSavedLayout ??= Default;
        }
    }

    public static AppLayoutDataV1 GetLayout()
    {
        lock (_lock)
        {
            return _lastSavedLayout ?? RestoreAppLayoutFromDisk();
        }
    }

    public static AppLayoutDataV1 SaveActionContextWidth(double width)
    {
        lock (_lock)
        {
            var current = _lastSavedLayout ?? RestoreAppLayoutFromDisk();
            var updated = current with { ActionContextWidth = Math.Max(200, width) };
            return SaveIfChanged(current, updated);
        }
    }

    public static AppLayoutDataV1 SaveTabPaletteWidth(double width)
    {
        lock (_lock)
        {
            var current = _lastSavedLayout ?? RestoreAppLayoutFromDisk();
            var updated = current with { TabPaletteWidth = Math.Max(200, width) };
            return SaveIfChanged(current, updated);
        }
    }

    private static AppLayoutDataV1 SaveIfChanged(AppLayoutDataV1 current, AppLayoutDataV1 updated)
    {
        if (current == updated)
            return current;

        try
        {
            var versioned = new PersistentData<AppLayoutDataV1>
            {
                Version = _currentVersion,
                Data = updated
            };
            File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versioned, _jsonSerializerOptions));
            _lastSavedLayout = updated;
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to save app layout: {e.Message}");
        }
        return _lastSavedLayout ?? updated;
    }
}
