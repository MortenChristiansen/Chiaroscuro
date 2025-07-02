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

        address = address.ToLowerInvariant();

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
            .Select(x => (Item: x, Score: GetRelevanceScore(x.Key, searchText)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Item.Key)
            .Take(maxSuggestions)
            .Select(x => new NavigationSuggestion(x.Item.Key, x.Item.Value.Title, x.Item.Value.Favicon))
            .ToList();

        return suggestions;
    }

    private static int GetRelevanceScore(string url, string searchText)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(searchText))
            return 0;

        var normalizedUrl = NormalizeAddress(url);
        var searchLower = searchText.ToLowerInvariant();

        // For strict prefix matching, search text must be a prefix of the URL
        if (!normalizedUrl.StartsWith(searchLower))
            return 0;

        // Return the length of the search text as the score
        return searchLower.Length;
    }
}

public record NavigationSuggestion(string Address, string Title, string? Favicon);