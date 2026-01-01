using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using Testably.Abstractions;

namespace BrowserHost.Features.AppState;

public record AppStateDataV1(double ActionContextWidth, double TabPaletteWidth);

public class AppStateStateManager
{
    public static string PersistedStatePath { get; } = AppDataPathManager.GetAppDataFilePath("appState.json");

    private readonly IFileSystem _fileSystem;
    private const int _currentVersion = 1;
    private AppStateDataV1? _lastSavedState;
    private readonly Lock _lock = new();

    private static AppStateDataV1 Default => new(ActionContextWidth: 300, TabPaletteWidth: 350);

    public AppStateStateManager() : this(new RealFileSystem())
    {
    }

    public AppStateStateManager(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public virtual AppStateDataV1 RestoreAppStateFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (_fileSystem.File.Exists(PersistedStatePath))
                {
                    var json = _fileSystem.File.ReadAllText(PersistedStatePath);
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

    public virtual AppStateDataV1 GetAppState()
    {
        lock (_lock)
        {
            return _lastSavedState ?? RestoreAppStateFromDisk();
        }
    }

    public virtual AppStateDataV1 SaveActionContextWidth(double width)
    {
        lock (_lock)
        {
            var current = _lastSavedState ?? RestoreAppStateFromDisk();
            var updated = current with { ActionContextWidth = Math.Max(200, width) };
            return SaveIfChanged(current, updated);
        }
    }

    public virtual AppStateDataV1 SaveTabPaletteWidth(double width)
    {
        lock (_lock)
        {
            var current = _lastSavedState ?? RestoreAppStateFromDisk();
            var updated = current with { TabPaletteWidth = Math.Max(200, width) };
            return SaveIfChanged(current, updated);
        }
    }

    private AppStateDataV1 SaveIfChanged(AppStateDataV1 current, AppStateDataV1 updated)
    {
        if (current == updated)
            return current;

        try
        {
            var stateDirectoryPath = _fileSystem.Path.GetDirectoryName(PersistedStatePath);
            if (!string.IsNullOrWhiteSpace(stateDirectoryPath))
            {
                _fileSystem.Directory.CreateDirectory(stateDirectoryPath);
            }

            var versioned = new PersistentData<AppStateDataV1>
            {
                Version = _currentVersion,
                Data = updated
            };
            _fileSystem.File.WriteAllText(PersistedStatePath, JsonSerializer.Serialize(versioned, BrowserHostJsonContext.Default.PersistentDataAppStateDataV1));
            _lastSavedState = updated;
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to save app state: {e.Message}");
        }
        return _lastSavedState ?? updated;
    }
}
