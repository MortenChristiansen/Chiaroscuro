using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace BrowserHost.Features.ActionDialog;

public record NavigationHistoryEntry(string Title, string? Favicon);

public static class NavigationHistoryStateManager
{
    private static readonly string _navigationHistoryPath = AppDataPathManager.GetAppDataFilePath("navigationHistory.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    // In-memory cache for navigation history
    private static Dictionary<string, NavigationHistoryEntry>? _cachedHistory = null;
    private static readonly Lock _cacheLock = new();

    public static void SaveNavigationEntry(string address, string? title, string? favicon)
    {
        var normalizedAddress = NormalizeAddress(address);

        try
        {
            MainWindow.Instance?.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"Saving navigation entry: {normalizedAddress}");

                lock (_cacheLock)
                {
                    EnsureCacheLoaded();
                    var newValue = new NavigationHistoryEntry(title ?? normalizedAddress, favicon);
                    if (_cachedHistory.TryGetValue(normalizedAddress, out var existingValue))
                    {
                        var isNewValue = newValue.Title == normalizedAddress && newValue.Favicon == null;
                        if (isNewValue) // We already have this entry and this is a new navigation without a title or favicon, so we should not update it
                            return;

                        if (existingValue == newValue) // No change, do not update
                            return;
                    }

                    _cachedHistory[normalizedAddress] = newValue;
                    File.WriteAllText(_navigationHistoryPath, JsonSerializer.Serialize(_cachedHistory, _jsonSerializerOptions));
                }
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to save navigation history: {e.Message}");
        }
    }

    private static string NormalizeAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return string.Empty;

        if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            address = address.Substring(7);
        else if (address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            address = address.Substring(8);

        if (address.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            address = address.Substring(4);

        return address.Trim().TrimEnd('/');
    }

    [MemberNotNull(nameof(_cachedHistory))]
    private static void EnsureCacheLoaded()
    {
        _cachedHistory ??= LoadNavigationHistoryFromDisk();
    }

    private static Dictionary<string, NavigationHistoryEntry> LoadNavigationHistoryFromDisk()
    {
        try
        {
            if (File.Exists(_navigationHistoryPath))
            {
                var json = File.ReadAllText(_navigationHistoryPath);
                return JsonSerializer.Deserialize<Dictionary<string, NavigationHistoryEntry>>(json) ?? new Dictionary<string, NavigationHistoryEntry>();
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to load navigation history: {e.Message}");
        }

        return new Dictionary<string, NavigationHistoryEntry>();
    }

    public static List<NavigationSuggestion> GetSuggestions(string searchText, int maxSuggestions = 5)
    {
        Dictionary<string, NavigationHistoryEntry> history;

        lock (_cacheLock)
        {
            EnsureCacheLoaded();
            history = _cachedHistory;
        }

        if (string.IsNullOrWhiteSpace(searchText))
            return [];

        var suggestions = history
            .Where(kvp => GetRelevanceScore(kvp.Key, searchText) > 0)
            .OrderByDescending(kvp => GetRelevanceScore(kvp.Key, searchText))
            .Take(maxSuggestions)
            .Select(kvp => new NavigationSuggestion(kvp.Key, kvp.Value.Title, kvp.Value.Favicon))
            .ToList();

        return suggestions;
    }

    private static int GetRelevanceScore(string url, string searchText)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(searchText))
            return 0;

        // Remove scheme from URL for matching (http://, https://)
        var urlWithoutScheme = url;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            urlWithoutScheme = url.Substring(7);
        else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            urlWithoutScheme = url.Substring(8);

        // Count matching characters
        var searchLower = searchText.ToLowerInvariant();
        var urlLower = urlWithoutScheme.ToLowerInvariant();

        int matchingChars = 0;
        int searchIndex = 0;

        for (int i = 0; i < urlLower.Length && searchIndex < searchLower.Length; i++)
        {
            if (urlLower[i] == searchLower[searchIndex])
            {
                matchingChars++;
                searchIndex++;
            }
        }

        // Return the number of matching characters
        return matchingChars;
    }
}

public record NavigationSuggestion(string Address, string Title, string? Favicon);