using BrowserHost.Logging;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record DomainCustomizationDataV1(string Domain, bool CssEnabled, bool HasCustomCss);

public static class DomainCustomizationStateManager
{
    private record DomainCustomizationSettingsV1(bool CssEnabled);
    private record DomainCustomizationSettingsV2(string Domain, bool CssEnabled);

    private const int _currentVersion = 2;
    private static readonly Lock _lock = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    // Cache customizations per domain on-demand only
    private static readonly Dictionary<string, DomainCustomizationDataV1> _cachedPerDomain = [];

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

    public static DomainCustomizationDataV1 GetCustomization(string domain)
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

    private static DomainCustomizationDataV1 LoadCustomization(string domain)
    {
        var filePath = GetCustomizationFilePath(domain);
        var cssPath = GetCssFilePath(domain);

        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var versioned = JsonSerializer.Deserialize<PersistentData>(json);
                if (versioned?.Version == _currentVersion)
                {
                    var hasCustomCss = File.Exists(cssPath);
                    var rawData = JsonSerializer.Deserialize<PersistentData<DomainCustomizationSettingsV2>>(json)?.Data;
                    var rawDomain = rawData?.Domain ?? domain;
                    return new DomainCustomizationDataV1(rawDomain, rawData?.CssEnabled ?? false, hasCustomCss);
                }
                else if (versioned?.Version == 1)
                {
                    var hasCustomCss = File.Exists(cssPath);
                    var rawData = JsonSerializer.Deserialize<PersistentData<DomainCustomizationSettingsV1>>(json)?.Data;
                    return new DomainCustomizationDataV1(domain, rawData?.CssEnabled ?? false, hasCustomCss);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load domain customization for {domain}: {ex.Message}");
        }

        // Return default values if no customization exists or loading failed
        var defaultHasCustomCss = File.Exists(cssPath);
        return new DomainCustomizationDataV1(domain, defaultHasCustomCss, defaultHasCustomCss);
    }

    public static DomainCustomizationDataV1? SaveCustomization(DomainCustomizationDataV1 customization)
    {
        lock (_lock)
        {
            try
            {
                var domainFolder = GetDomainFolder(customization.Domain);
                Directory.CreateDirectory(domainFolder);

                var data = new PersistentData<DomainCustomizationSettingsV2>
                {
                    Version = _currentVersion,
                    Data = new DomainCustomizationSettingsV2(customization.Domain, customization.CssEnabled)
                };
                var json = JsonSerializer.Serialize(data, _jsonSerializerOptions);

                File.WriteAllText(GetCustomizationFilePath(customization.Domain), json);

                var updated = customization with { HasCustomCss = File.Exists(GetCssFilePath(customization.Domain)) };
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

    public static string GetCustomCssPath(string domain)
    {
        return GetCssFilePath(domain);
    }

    public static string? GetCustomCss(string domain)
    {
        var cssPath = GetCssFilePath(domain);
        if (File.Exists(cssPath))
        {
            try
            {
                return File.ReadAllText(cssPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read CSS for domain {domain}: {ex.Message}");
            }
        }
        return null;
    }

    public static void RefreshCacheForDomain(string domain)
    {
        lock (_lock)
        {
            _cachedPerDomain.Remove(CacheKey(domain));
        }
    }

    public static IReadOnlyCollection<DomainCustomizationDataV1> GetAllCustomizations()
    {
        using (Measure.Operation("Restoring domain customizations from disk"))
        {
            lock (_lock)
            {
                var results = new List<DomainCustomizationDataV1>();

                try
                {
                    if (Directory.Exists(RootFolder))
                    {
                        foreach (var dir in Directory.EnumerateDirectories(RootFolder))
                        {
                            var settingsFile = Path.Combine(dir, "settings.json");
                            var cssFile = Path.Combine(dir, "custom.css");

                            var sanitizedDomainName = Path.GetFileName(dir);
                            var hasCustomCss = File.Exists(cssFile);
                            bool cssEnabled = hasCustomCss; // Default to enabled if CSS exists
                            var rawDomain = sanitizedDomainName; // Fallback for legacy entries

                            if (File.Exists(settingsFile))
                            {
                                try
                                {
                                    var json = File.ReadAllText(settingsFile);
                                    var versioned = JsonSerializer.Deserialize<PersistentData>(json);
                                    if (versioned?.Version == _currentVersion)
                                    {
                                        var rawData = JsonSerializer.Deserialize<PersistentData<DomainCustomizationSettingsV2>>(json)?.Data;
                                        if (rawData is not null)
                                        {
                                            cssEnabled = rawData.CssEnabled;
                                            rawDomain = rawData.Domain;
                                        }
                                    }
                                    else if (versioned?.Version == 1)
                                    {
                                        var rawData = JsonSerializer.Deserialize<PersistentData<DomainCustomizationSettingsV1>>(json)?.Data;
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
}