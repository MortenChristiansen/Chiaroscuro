using BrowserHost.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.Settings;

public record SettingsDataV1(string? UserAgent, string[]? SsoEnabledDomains);

public static class SettingsStateManager
{
    private static readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("settings.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };
    private const int _currentVersion = 1;
    private static SettingsDataV1? _lastSavedSettingsData = null;
    private static readonly Lock _lock = new();

    public static SettingsDataV1 SaveSettings(SettingsDataV1 settings)
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
                var versionedData = new PersistentData<SettingsDataV1>
                {
                    Version = _currentVersion,
                    Data = settings
                };
                File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versionedData, _jsonSerializerOptions));
                _lastSavedSettingsData = settings;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to save settings state: {e.Message}");
            }
            return _lastSavedSettingsData!;
        }
    }

    public static SettingsDataV1 RestoreSettingsFromDisk()
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
                        var data = JsonSerializer.Deserialize<PersistentData<SettingsDataV1>>(json)?.Data;
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
            return _lastSavedSettingsData ?? new SettingsDataV1(null, []);
        }
    }

    private static bool StateIsEqual(SettingsDataV1? data1, SettingsDataV1? data2)
    {
        if (data1 is null || data2 is null) return false;
        return data1 == data2;
    }
}
