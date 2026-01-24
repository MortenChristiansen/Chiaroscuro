using BrowserHost.Logging;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.TabPalette.LocalWebApp;

public record LocalWebAppConfigV1(string TabId, string DirectoryPath, string? StartCommand);
public record LocalWebAppDataV1(LocalWebAppConfigV1[] Configs);

public class LocalWebAppStateManager(IFileSystem fileSystem)
{
    public static string PersistedStatePath { get; } = AppDataPathManager.GetAppDataFilePath("local-web-apps.json");

    private const int _currentVersion = 1;
    private readonly Lock _lock = new();
    private readonly Dictionary<string, LocalWebAppConfigV1> _configs = [];

    public virtual LocalWebAppConfigV1? GetConfig(string tabId)
    {
        lock (_lock)
        {
            return _configs.TryGetValue(tabId, out var config) ? config : null;
        }
    }

    public virtual IReadOnlyCollection<LocalWebAppConfigV1> GetAllConfigs()
    {
        lock (_lock)
        {
            return [.. _configs.Values];
        }
    }

    public virtual LocalWebAppConfigV1 SaveConfig(LocalWebAppConfigV1 config)
    {
        lock (_lock)
        {
            _configs[config.TabId] = config;
            PersistToDisk();
            return config;
        }
    }

    public virtual bool DeleteConfig(string tabId)
    {
        lock (_lock)
        {
            if (_configs.Remove(tabId))
            {
                PersistToDisk();
                return true;
            }
            return false;
        }
    }

    public virtual void RestoreFromDisk()
    {
        using (Measure.Operation("Restoring local web app configs from disk"))
        {
            lock (_lock)
            {
                try
                {
                    if (fileSystem.File.Exists(PersistedStatePath))
                    {
                        var json = fileSystem.File.ReadAllText(PersistedStatePath);
                        var versionedData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentData);
                        if (versionedData?.Version == _currentVersion)
                        {
                            var data = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataLocalWebAppDataV1)?.Data;
                            if (data?.Configs != null)
                            {
                                _configs.Clear();
                                foreach (var config in data.Configs)
                                {
                                    _configs[config.TabId] = config;
                                }
                            }
                        }
                    }
                }
                catch (Exception e) when (!Debugger.IsAttached)
                {
                    Debug.WriteLine($"Failed to restore local web app configs: {e.Message}");
                }
            }
        }
    }

    private void PersistToDisk()
    {
        try
        {
            var stateDirectoryPath = fileSystem.Path.GetDirectoryName(PersistedStatePath);
            if (!string.IsNullOrWhiteSpace(stateDirectoryPath))
            {
                fileSystem.Directory.CreateDirectory(stateDirectoryPath);
            }

            var data = new LocalWebAppDataV1([.. _configs.Values]);
            var versionedData = new PersistentData<LocalWebAppDataV1>
            {
                Version = _currentVersion,
                Data = data
            };
            fileSystem.File.WriteAllText(PersistedStatePath, JsonSerializer.Serialize(versionedData, BrowserHostJsonContext.Default.PersistentDataLocalWebAppDataV1));
        }
        catch (Exception e) when (!Debugger.IsAttached)
        {
            Debug.WriteLine($"Failed to save local web app configs: {e.Message}");
        }
    }
}
