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
    private const int _currentVersion = 1;
    private static readonly Lock _lock = new();
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    // Cache customizations per domain on-demand only
    private static readonly Dictionary<string, DomainCustomizationDataV1> _cachedPerDomain = [];

    private static string RootFolder => Path.Combine(AppDataPathManager.GetAppDataFolderPath(), "DomainSettings");
    private static string GetDomainFolder(string domain) => Path.Combine(RootFolder, SanitizeDomainName(domain));
    private static string GetCustomizationFilePath(string domain) => Path.Combine(GetDomainFolder(domain), "settings.json");
    private static string GetCssFilePath(string domain) => Path.Combine(GetDomainFolder(domain), "custom.css");

    // Keep domain folder names simple/safe for filesystem
    private static string SanitizeDomainName(string domain)
    {
        var sanitized = domain.ToLowerInvariant();
        foreach (var c in Path.GetInvalidFileNameChars())
            sanitized = sanitized.Replace(c, '_');
        // Also replace other problematic characters
        sanitized = sanitized.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
        return sanitized;
    }

    public static DomainCustomizationDataV1 GetCustomization(string domain)
    {
        lock (_lock)
        {
            if (_cachedPerDomain.TryGetValue(domain, out var cached))
                return cached;

            var customization = LoadCustomization(domain);
            _cachedPerDomain[domain] = customization;
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
                if (versioned?.Version == _currentVersion && versioned.Data != null)
                {
                    var hasCustomCss = File.Exists(cssPath);
                    return new DomainCustomizationDataV1(domain, versioned.Data.CssEnabled, hasCustomCss);
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

                var data = new PersistentDataV1 { CssEnabled = customization.CssEnabled };
                var versioned = new PersistentData { Version = _currentVersion, Data = data };
                var json = JsonSerializer.Serialize(versioned, _jsonSerializerOptions);
                
                File.WriteAllText(GetCustomizationFilePath(customization.Domain), json);

                // Update cache
                var updated = customization with { HasCustomCss = File.Exists(GetCssFilePath(customization.Domain)) };
                _cachedPerDomain[customization.Domain] = updated;
                
                return updated;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save domain customization for {customization.Domain}: {ex.Message}");
                return null;
            }
        }
    }

    public static void DeleteCustomization(string domain)
    {
        lock (_lock)
        {
            try
            {
                var domainFolder = GetDomainFolder(domain);
                if (Directory.Exists(domainFolder))
                    Directory.Delete(domainFolder, recursive: true);

                _cachedPerDomain.Remove(domain);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete domain customization for {domain}: {ex.Message}");
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
            _cachedPerDomain.Remove(domain);
        }
    }

    public static IReadOnlyCollection<DomainCustomizationDataV1> GetAllCustomizations()
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
                        
                        var domainName = Path.GetFileName(dir);
                        var hasCustomCss = File.Exists(cssFile);
                        bool cssEnabled = hasCustomCss; // Default to enabled if CSS exists

                        if (File.Exists(settingsFile))
                        {
                            try
                            {
                                var json = File.ReadAllText(settingsFile);
                                var versioned = JsonSerializer.Deserialize<PersistentData>(json);
                                if (versioned?.Version == _currentVersion && versioned.Data != null)
                                {
                                    cssEnabled = versioned.Data.CssEnabled;
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to parse settings for domain {domainName}: {ex.Message}");
                            }
                        }

                        var customization = new DomainCustomizationDataV1(domainName, cssEnabled, hasCustomCss);
                        results.Add(customization);
                        
                        // Update cache while we're at it
                        _cachedPerDomain[domainName] = customization;
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

    private record PersistentData
    {
        public int Version { get; set; }
        public PersistentDataV1? Data { get; set; }
    }

    private record PersistentDataV1
    {
        public bool CssEnabled { get; set; }
    }
}