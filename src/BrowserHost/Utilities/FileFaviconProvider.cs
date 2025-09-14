using System;
using System.Collections.Generic;
using System.IO;

namespace BrowserHost.Utilities;

public static class FileFaviconProvider
{
    private static readonly Dictionary<string, (string label, string color)> _map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        ["pdf"] = ("PDF", "#EF4444"),
        ["doc"] = ("DOC", "#3B82F6"),
        ["docx"] = ("DOC", "#3B82F6"),
        ["rtf"] = ("RTF", "#60A5FA"),
        ["odt"] = ("ODT", "#60A5FA"),

        // Spreadsheets
        ["xls"] = ("XLS", "#22C55E"),
        ["xlsx"] = ("XLS", "#22C55E"),
        ["csv"] = ("CSV", "#16A34A"),
        ["ods"] = ("ODS", "#16A34A"),

        // Presentations
        ["ppt"] = ("PPT", "#F97316"),
        ["pptx"] = ("PPT", "#F97316"),
        ["odp"] = ("ODP", "#FB923C"),

        // Code / data
        ["json"] = ("JSON", "#EAB308"),
        ["xml"] = ("XML", "#A855F7"),
        ["yml"] = ("YML", "#A78BFA"),
        ["yaml"] = ("YAML", "#A78BFA"),
        ["txt"] = ("TXT", "#9CA3AF"),
        ["log"] = ("LOG", "#9CA3AF"),
        ["md"] = ("MD", "#64748B"),
        ["html"] = ("HTML", "#0EA5E9"),
        ["htm"] = ("HTML", "#0EA5E9"),
        ["css"] = ("CSS", "#38BDF8"),
        ["js"] = ("JS", "#F59E0B"),
        ["ts"] = ("TS", "#3B82F6"),
        ["cs"] = ("CS", "#22C55E"),

        // Images
        ["jpg"] = ("IMG", "#8B5CF6"),
        ["jpeg"] = ("IMG", "#8B5CF6"),
        ["png"] = ("IMG", "#8B5CF6"),
        ["gif"] = ("IMG", "#8B5CF6"),
        ["bmp"] = ("IMG", "#8B5CF6"),
        ["webp"] = ("IMG", "#8B5CF6"),
        ["tiff"] = ("IMG", "#8B5CF6"),
        ["svg"] = ("IMG", "#8B5CF6"),

        // Audio
        ["mp3"] = ("AUD", "#10B981"),
        ["wav"] = ("AUD", "#10B981"),
        ["flac"] = ("AUD", "#10B981"),
        ["ogg"] = ("AUD", "#10B981"),

        // Video
        ["mp4"] = ("VID", "#14B8A6"),
        ["mov"] = ("VID", "#14B8A6"),
        ["avi"] = ("VID", "#14B8A6"),
        ["mkv"] = ("VID", "#14B8A6"),
        ["webm"] = ("VID", "#14B8A6"),

        // Executables / installers
        ["bat"] = ("APP", "#6B7280"),
        ["cmd"] = ("APP", "#6B7280"),
        ["ps1"] = ("APP", "#6B7280"),
        ["sh"] = ("APP", "#6B7280"),
    };

    public static string? TryGetFaviconForAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        if (!IsFileAddress(address)) return null;

        var ext = GetExtension(address);
        if (string.IsNullOrEmpty(ext)) return CreateLabelIcon("FILE", "#9CA3AF");

        if (_map.TryGetValue(ext, out var info))
            return CreateLabelIcon(info.label, info.color);

        return CreateLabelIcon(ext.Length <= 4 ? ext.ToUpperInvariant() : "FILE", "#9CA3AF");
    }

    private static bool IsFileAddress(string address)
    {
        if (Uri.TryCreate(address, UriKind.Absolute, out var uri))
            return uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase);

        // Also treat plain paths as file addresses
        try
        {
            return Path.IsPathRooted(address) || address.StartsWith(".\\") || address.StartsWith("./");
        }
        catch
        {
            return false;
        }
    }

    private static string GetExtension(string address)
    {
        try
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out var uri) && uri.IsFile)
                return Path.GetExtension(Uri.UnescapeDataString(uri.LocalPath)).TrimStart('.').ToLowerInvariant();
            return Path.GetExtension(address).TrimStart('.');
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string CreateLabelIcon(string label, string colorHex)
    {
        var bg = colorHex.Replace("#", "%23");
        // Rounded rect background with centered label
        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' viewBox='0 0 16 16'><rect width='16' height='16' rx='4' fill='{bg}'/><text x='8' y='11' text-anchor='middle' font-size='7' fill='white' font-family='Arial, Helvetica, sans-serif'>{label}</text></svg>";
        return "data:image/svg+xml;utf8," + svg;
    }
}
