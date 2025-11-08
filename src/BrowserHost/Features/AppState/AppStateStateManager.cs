using BrowserHost.Utilities;
using BrowserHost.Serialization;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.AppState;

public record AppStateDataV1(double ActionContextWidth, double TabPaletteWidth);

public static class AppStateStateManager
{
    private static readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("appState.json");
    private const int _currentVersion = 1;
    private static AppStateDataV1? _lastSavedState;
    private static readonly Lock _lock = new();

    private static AppStateDataV1 Default => new(ActionContextWidth: 300, TabPaletteWidth: 350);

    public static AppStateDataV1 RestoreAppStateFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_persistedStatePath))
                {
                    var json = File.ReadAllText(_persistedStatePath);
                    var versioned = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentData);
                    if (versioned?.Version == _currentVersion)
                    {
                        var parsed = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataAppStateDataV1);
                        if (parsed?.Data is not null)
                            _lastSavedState = parsed.Data;
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to restore app state: {e.Message}");
            }

            return _lastSavedState ??= Default;
        }
    }

    public static AppStateDataV1 GetAppState()
    {
        lock (_lock)
        {
            return _lastSavedState ?? RestoreAppStateFromDisk();
        }
    }

    public static AppStateDataV1 SaveActionContextWidth(double width)
    {
        lock (_lock)
        {
            var current = _lastSavedState ?? RestoreAppStateFromDisk();
            var updated = current with { ActionContextWidth = Math.Max(200, width) };
            return SaveIfChanged(current, updated);
        }
    }

    public static AppStateDataV1 SaveTabPaletteWidth(double width)
    {
        lock (_lock)
        {
            var current = _lastSavedState ?? RestoreAppStateFromDisk();
            var updated = current with { TabPaletteWidth = Math.Max(200, width) };
            return SaveIfChanged(current, updated);
        }
    }

    private static AppStateDataV1 SaveIfChanged(AppStateDataV1 current, AppStateDataV1 updated)
    {
        if (current == updated)
            return current;

        try
        {
            var versioned = new PersistentData<AppStateDataV1>
            {
                Version = _currentVersion,
                Data = updated
            };
            File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versioned, BrowserHostJsonContext.Default.PersistentDataAppStateDataV1));
            _lastSavedState = updated;
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to save app state: {e.Message}");
        }
        return _lastSavedState ?? updated;
    }
}
