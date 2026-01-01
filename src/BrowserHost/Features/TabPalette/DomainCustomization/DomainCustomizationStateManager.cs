using BrowserHost.Logging;
using BrowserHost.Serialization;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.IO.Abstractions;
using Testably.Abstractions;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record DomainCustomizationDataV1(string Domain, bool CssEnabled, bool HasCustomCss);

// These should have been private to the DomainCustomizationStateManager class, but that did not work with serialization using source generation.
public record DomainCustomizationSettingsV1(bool CssEnabled);
public record DomainCustomizationSettingsV2(string Domain, bool CssEnabled);

public class DomainCustomizationStateManager
{
    private readonly IFileSystem _fileSystem;
    private const int _currentVersion = 2;
    private readonly Lock _lock = new();

    // Cache customizations per domain on-demand only
    private readonly Dictionary<string, DomainCustomizationDataV1> _cachedPerDomain = [];

    private static string RootFolder => Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "domain-settings");
    private static string GetDomainFolder(string domain) => Path.Combine(RootFolder, SanitizeDomainName(domain));
    private static string GetCustomizationFilePath(string domain) => Path.Combine(GetDomainFolder(domain), "settings.json");
    private static string GetCssFilePath(string domain) => Path.Combine(GetDomainFolder(domain), "custom.css");

    private static string SanitizeDomainName(string domain)
    {
        var sanitized = domain.ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(c, '_');
        // Also replace other problematic characters
        sanitized = sanitized.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
        return sanitized;
    }

    private static string CacheKey(string domain) => SanitizeDomainName(domain);

    public DomainCustomizationStateManager() : this(new RealFileSystem())
    {
    }

    public DomainCustomizationStateManager(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public virtual DomainCustomizationDataV1 GetCustomization(string domain)
    {
        lock (_lock)
        {
            var key = CacheKey(domain);
            if (_cachedPerDomain.TryGetValue(key, out var cached))
                return cached;

            var customization = LoadCustomization(domain);
            _cachedPerDomain[key] = customization;
            return customization;
        }
    }

    private DomainCustomizationDataV1 LoadCustomization(string domain)
    {
        var filePath = GetCustomizationFilePath(domain);
        var cssPath = GetCssFilePath(domain);

        try
        {
            if (_fileSystem.File.Exists(filePath))
            {
                var json = _fileSystem.File.ReadAllText(filePath);
                var versioned = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentData);
                if (versioned?.Version == _currentVersion)
                {
                    var hasCustomCss = _fileSystem.File.Exists(cssPath);
                    var rawData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataDomainCustomizationSettingsV2)?.Data;
                    var rawDomain = rawData?.Domain ?? domain;
                    return new DomainCustomizationDataV1(rawDomain, rawData?.CssEnabled ?? false, hasCustomCss);
                }
                else if (versioned?.Version == 1)
                {
                    var hasCustomCss = _fileSystem.File.Exists(cssPath);
                    var rawData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataDomainCustomizationSettingsV1)?.Data;
                    return new DomainCustomizationDataV1(domain, rawData?.CssEnabled ?? false, hasCustomCss);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load domain customization for {domain}: {ex.Message}");
        }

        // Return default values if no customization exists or loading failed
        var defaultHasCustomCss = _fileSystem.File.Exists(cssPath);
        return new DomainCustomizationDataV1(domain, defaultHasCustomCss, defaultHasCustomCss);
    }

    public virtual DomainCustomizationDataV1? SaveCustomization(DomainCustomizationDataV1 customization)
    {
        lock (_lock)
        {
            try
            {
                var domainFolder = GetDomainFolder(customization.Domain);
                _fileSystem.Directory.CreateDirectory(domainFolder);

                var data = new PersistentData<DomainCustomizationSettingsV2>
                {
                    Version = _currentVersion,
                    Data = new DomainCustomizationSettingsV2(customization.Domain, customization.CssEnabled)
                };
                var json = JsonSerializer.Serialize(data, BrowserHostJsonContext.Default.PersistentDataDomainCustomizationSettingsV2);

                _fileSystem.File.WriteAllText(GetCustomizationFilePath(customization.Domain), json);

                var updated = customization with { HasCustomCss = _fileSystem.File.Exists(GetCssFilePath(customization.Domain)) };
                _cachedPerDomain[CacheKey(customization.Domain)] = updated;

                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save domain customization for {customization.Domain}: {ex.Message}");
                return null;
            }
        }
    }

    public virtual string? GetCustomCss(string domain)
    {
        var cssPath = GetCssFilePath(domain);
        if (_fileSystem.File.Exists(cssPath))
        {
            try
            {
                return _fileSystem.File.ReadAllText(cssPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read CSS for domain {domain}: {ex.Message}");
            }
        }
        return null;
    }

    public virtual void RefreshCacheForDomain(string domain)
    {
        lock (_lock)
        {
            _cachedPerDomain.Remove(CacheKey(domain));
        }
    }

    public virtual void RemoveCustomCss(string domain)
    {
        try
        {
            var cssPath = GetCssFilePath(domain);
            if (_fileSystem.File.Exists(cssPath))
                _fileSystem.File.Delete(cssPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to remove CSS for domain {domain}: {ex.Message}");
        }
        finally
        {
            RefreshCacheForDomain(domain);
        }
    }

    public virtual bool EnsureCustomCssExistsAndOpenInEditor(string domain)
    {
        try
        {
            var cssPath = GetCssFilePath(domain);
            var domainFolder = Path.GetDirectoryName(cssPath);
            if (string.IsNullOrEmpty(domainFolder))
                return false;

            _fileSystem.Directory.CreateDirectory(domainFolder);

            if (!_fileSystem.File.Exists(cssPath))
            {
                _fileSystem.File.WriteAllText(cssPath, $"/* Custom CSS for {domain} */\n\n");
            }

            var processStartInfo = new ProcessStartInfo(cssPath)
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo);

            RefreshCacheForDomain(domain);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open CSS editor for domain {domain}: {ex.Message}");
            return false;
        }
    }

    public enum CustomCssWatchEventKind
    {
        Changed,
        Removed,
    }

    public virtual IDisposable WatchCustomCss(string domain, Action<CustomCssWatchEventKind> onEvent)
    {
        try
        {
            var cssPath = GetCssFilePath(domain);
            var directory = Path.GetDirectoryName(cssPath);
            var fileName = Path.GetFileName(cssPath);

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(fileName) || !_fileSystem.Directory.Exists(directory))
                return NoopDisposable.Instance;

            var watcher = _fileSystem.FileSystemWatcher.New(directory, fileName);
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
            watcher.EnableRaisingEvents = true;

            void handleChange()
            {
                try
                {
                    // Small delay to ensure file write is complete
                    Thread.Sleep(100);

                    if (!_fileSystem.File.Exists(cssPath))
                    {
                        onEvent(CustomCssWatchEventKind.Removed);
                        return;
                    }

                    onEvent(CustomCssWatchEventKind.Changed);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to handle CSS file change for domain {domain}: {ex.Message}");
                }
            }

            watcher.Changed += (_, e) =>
            {
                if (string.Equals(e.FullPath, cssPath, StringComparison.OrdinalIgnoreCase))
                    handleChange();
            };

            watcher.Deleted += (_, e) =>
            {
                if (string.Equals(e.FullPath, cssPath, StringComparison.OrdinalIgnoreCase))
                    onEvent(CustomCssWatchEventKind.Removed);
            };

            watcher.Renamed += (_, e) =>
            {
                if (string.Equals(e.OldFullPath, cssPath, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(e.FullPath, cssPath, StringComparison.OrdinalIgnoreCase))
                {
                    onEvent(CustomCssWatchEventKind.Removed);
                }
            };

            return watcher;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to setup CSS file watcher for domain {domain}: {ex.Message}");
            return NoopDisposable.Instance;
        }
    }

    public virtual IReadOnlyCollection<DomainCustomizationDataV1> GetAllCustomizations()
    {
        using (Measure.Operation("Restoring domain customizations from disk"))
        {
            lock (_lock)
            {
                var results = new List<DomainCustomizationDataV1>();

                try
                {
                    if (_fileSystem.Directory.Exists(RootFolder))
                    {
                        foreach (var dir in _fileSystem.Directory.EnumerateDirectories(RootFolder))
                        {
                            var settingsFile = Path.Combine(dir, "settings.json");
                            var cssFile = Path.Combine(dir, "custom.css");

                            var sanitizedDomainName = Path.GetFileName(dir);
                            var hasCustomCss = _fileSystem.File.Exists(cssFile);
                            bool cssEnabled = hasCustomCss; // Default to enabled if CSS exists
                            var rawDomain = sanitizedDomainName; // Fallback for legacy entries

                            if (_fileSystem.File.Exists(settingsFile))
                            {
                                try
                                {
                                    var json = _fileSystem.File.ReadAllText(settingsFile);
                                    var versioned = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentData);
                                    if (versioned?.Version == _currentVersion)
                                    {
                                        var rawData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataDomainCustomizationSettingsV2)?.Data;
                                        if (rawData is not null)
                                        {
                                            cssEnabled = rawData.CssEnabled;
                                            rawDomain = rawData.Domain;
                                        }
                                    }
                                    else if (versioned?.Version == 1)
                                    {
                                        var rawData = JsonSerializer.Deserialize(json, BrowserHostJsonContext.Default.PersistentDataDomainCustomizationSettingsV1)?.Data;
                                        cssEnabled = rawData?.CssEnabled ?? cssEnabled;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Failed to parse settings for domain {sanitizedDomainName}: {ex.Message}");
                                }
                            }

                            var customization = new DomainCustomizationDataV1(rawDomain, cssEnabled, hasCustomCss);
                            results.Add(customization);

                            // Update cache while we're at it (use sanitized cache key)
                            _cachedPerDomain[CacheKey(rawDomain)] = customization;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to enumerate domain customizations: {ex.Message}");
                }

                return results;
            }
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static NoopDisposable Instance { get; } = new();
        public void Dispose() { }
    }
}