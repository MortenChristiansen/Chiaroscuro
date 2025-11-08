using System.Text.Json;
using BrowserHost.Serialization;

namespace BrowserHost.Utilities;

public static class JsonExtensions
{
    public static string ToJsonString(this string? s) =>
        s == null ? "null" : $"'{s.Replace("'", "\\'")}'";

    public static string ToJsonBoolean(this bool? b) =>
        b.HasValue ? b.Value.ToJsonBoolean() : "null";

    public static string ToJsonBoolean(this bool b) =>
        b.ToString().ToLowerInvariant();

    public static string ToJsonObject(this object? obj) =>
        obj == null ? "null" : JsonSerializer.Serialize(obj, BrowserHostCamelCaseJsonContext.Default.Object);
}
