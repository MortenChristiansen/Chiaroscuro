using BrowserHost.Utilities;
using BrowserHost.Serialization;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.IO.Abstractions;
using Testably.Abstractions;

namespace BrowserHost.Features.ActionContext.PinnedTabs;

public record PinnedTabDataV1(PinnedTabDtoV1[] PinnedTabs, string? ActiveTabId);
public record PinnedTabDtoV1(string Id, string? Title, string? Favicon, string Address);

public class PinnedTabsStateManager
{
    private readonly IFileSystem _fileSystem;
    private readonly string _persistedStatePath = AppDataPathManager.GetAppDataFilePath("pinned_tabs.json");
    private const int _currentVersion = 1;
    private PinnedTabDataV1 _lastSavedPinnedTabsData = new([], null);
    private readonly Lock _lock = new();

    public PinnedTabsStateManager() : this(new RealFileSystem())
    {
    }

    public PinnedTabsStateManager(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public virtual PinnedTabDataV1 SavePinnedTabs(PinnedTabDataV1 pinnedTabsData)
    {
        lock (_lock)
        {
            if (StateIsEqual(_lastSavedPinnedTabsData, pinnedTabsData))
            {
                Debug.WriteLine("Skipping pinned tabs state save - no changes detected.");
                return _lastSavedPinnedTabsData;
            }

            try
            {
                var stateDirectoryPath = _fileSystem.Path.GetDirectoryName(_persistedStatePath);
                if (!string.IsNullOrWhiteSpace(stateDirectoryPath))
                {
                    _fileSystem.Directory.CreateDirectory(stateDirectoryPath);
                }

                var versionedData = new PersistentData<PinnedTabDataV1>
                {
                    Version = _currentVersion,
                    Data = pinnedTabsData
                };
                _fileSystem.File.WriteAllText(_persistedStatePath, JsonSerializer.Serialize(versionedData, BrowserHostJsonContext.Default.PersistentDataPinnedTabDataV1));
                _lastSavedPinnedTabsData = pinnedTabsData;
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to save pinned tabs state: {e.Message}");
            }
            return _lastSavedPinnedTabsData;
        }
    }

    public virtual PinnedTabDataV1 RestorePinnedTabsFromDisk()
    {
        lock (_lock)
        {
            try
            {
                if (_fileSystem.File.Exists(_persistedStatePath))
                {
                    var json = _fileSystem.File.ReadAllText(_persistedStatePath);
                    var versionedData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentData);
                    if (versionedData?.Version == _currentVersion)
                    {
                        var data = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataPinnedTabDataV1)?.Data;
                        if (data != null)
                        {
                            _lastSavedPinnedTabsData = data;
                        }
                    }
                }
            }
            catch (Exception e) when (!Debugger.IsAttached)
            {
                Debug.WriteLine($"Failed to restore pinned tabs state: {e.Message}");
            }
            return _lastSavedPinnedTabsData;
        }
    }

    private static bool StateIsEqual(PinnedTabDataV1 data1, PinnedTabDataV1 data2)
    {
        if (ReferenceEquals(data1, data2)) return true;
        if (data1 is null || data2 is null) return false;
        if (data1.ActiveTabId != data2.ActiveTabId) return false;
        if (data1.PinnedTabs.Length != data2.PinnedTabs.Length) return false;

        for (int i = 0; i < data1.PinnedTabs.Length; i++)
        {
            if (!data1.PinnedTabs[i].Equals(data2.PinnedTabs[i]))
                return false;
        }

        return true;
    }
}
