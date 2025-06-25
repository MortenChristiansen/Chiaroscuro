using System.Text.Json;

namespace BrowserHost.Utilities;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _tabSerializationOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string ToJsonString(this string? s) =>
        s == null ? "null" : $"'{s}'";

    public static string ToJsonBoolean(this bool? b) =>
        b.HasValue ? b.Value.ToJsonBoolean() : "null";

    public static string ToJsonBoolean(this bool b) =>
        b.ToString().ToLowerInvariant();

    public static string ToJsonObject(this object? obj) =>
        obj == null ? "null" : JsonSerializer.Serialize(obj, _tabSerializationOptions);
}
