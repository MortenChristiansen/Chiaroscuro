using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace BrowserHost.Features.ActionDialog;

public record NavigationHistoryEntry(string Title, string? Favicon);

public static class NavigationHistoryStateManager
{
    private static readonly string _navigationHistoryPath = AppDataPathManager.GetAppDataFilePath("navigationHistory.json");
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

    public static void SaveNavigationEntry(string address, string? title, string? favicon)
    {
        try
        {
            MainWindow.Instance?.Dispatcher.Invoke(() =>
            {
                Debug.WriteLine($"Saving navigation entry: {address}");
                
                var history = LoadNavigationHistory();
                
                // Update or add the entry (address is the key)
                history[address] = new NavigationHistoryEntry(title ?? address, favicon);
                
                File.WriteAllText(_navigationHistoryPath, JsonSerializer.Serialize(history, _jsonSerializerOptions));
            });
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to save navigation history: {e.Message}");
        }
    }

    public static Dictionary<string, NavigationHistoryEntry> LoadNavigationHistory()
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
        var history = LoadNavigationHistory();
        
        if (string.IsNullOrWhiteSpace(searchText))
            return new List<NavigationSuggestion>();

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