using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;
using Testably.Abstractions;

namespace BrowserHost.Features.Settings;

public record SettingsDataV1(string? UserAgent, string[]? SsoEnabledDomains, bool? AutoAddSsoDomains);

public class SettingsStateManager
{
    public static string PersistedStatePath { get; } = AppDataPathManager.GetAppDataFilePath("settings.json");

    private readonly IFileSystem _fileSystem;
    private const int _currentVersion = 1;
    private SettingsDataV1? _lastSavedSettingsData = null;
    private readonly Lock _lock = new();

    public SettingsStateManager() : this(new RealFileSystem())
    {
    }

    public SettingsStateManager(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public virtual SettingsDataV1 SaveSettings(SettingsDataV1 settings)
    {
        lock (_lock)
        {
            if (StateIsEqual(_lastSavedSettingsData, settings))
            {
                Debug.WriteLine("Skipping settings state save - no changes detected.");
                return _lastSavedSettingsData!;
            }

            try
            {
                var stateDirectoryPath = _fileSystem.Path.GetDirectoryName(PersistedStatePath);
                if (!string.IsNullOrWhiteSpace(stateDirectoryPath))
                {
                    _fileSystem.Directory.CreateDirectory(stateDirectoryPath);
                }

                var versionedData = new PersistentData<SettingsDataV1>
                {
                    Version = _currentVersion,
                    Data = settings
                };
                _fileSystem.File.WriteAllText(PersistedStatePath, JsonSerializer.Serialize(versionedData, BrowserHostJsonContext.Default.PersistentDataSettingsDataV1));
                _lastSavedSettingsData = settings;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to save settings state: {e.Message}");
            }
            return _lastSavedSettingsData!;
        }
    }

    public virtual SettingsDataV1 RestoreSettingsFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (_fileSystem.File.Exists(PersistedStatePath))
                {
                    var json = _fileSystem.File.ReadAllText(PersistedStatePath);
                    var versionedData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentData);
                    if (versionedData?.Version == _currentVersion)
                    {
                        var data = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataSettingsDataV1)?.Data;
                        if (data != null)
                        {
                            _lastSavedSettingsData = data;
                        }
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to restore settings state: {e.Message}");
            }
            return _lastSavedSettingsData ?? new SettingsDataV1(null, [], false);
        }
    }

    private static bool StateIsEqual(SettingsDataV1? a, SettingsDataV1 b)
    {
        if (a is null) return false;
        if (a.UserAgent != b.UserAgent)
            return false;
        if (!DataComparisons.AreArraysEqual(a.SsoEnabledDomains, b.SsoEnabledDomains))
            return false;
        if (a.AutoAddSsoDomains != b.AutoAddSsoDomains)
            return false;
        return true;
    }
}
