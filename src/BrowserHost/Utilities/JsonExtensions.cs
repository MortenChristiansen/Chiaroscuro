using BrowserHost.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace BrowserHost.Utilities;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _camelCaseWithFallback = CreateCamelCaseWithFallback();

    private static JsonSerializerOptions CreateCamelCaseWithFallback()
    {
        // Start from the source-generated camelCase options and add reflection fallback
        var opts = new JsonSerializerOptions(BrowserHostCamelCaseJsonContext.Default.Options)
        {
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                BrowserHostCamelCaseJsonContext.Default.Options.TypeInfoResolver!,
                new DefaultJsonTypeInfoResolver())
        };
        return opts;
    }

    public static string ToJsonString(this string? s) =>
        s == null ? "null" : $"'{s.Replace("'", "\\'")}'";

    public static string ToJsonBoolean(this bool? b) =>
        b.HasValue ? b.Value.ToJsonBoolean() : "null";

    public static string ToJsonBoolean(this bool b) =>
        b.ToString().ToLowerInvariant();

    // Use source-generated metadata when available; otherwise fall back to reflection, preserving camelCase.
    public static string ToJsonObject(this object? obj) =>
        obj == null ? "null" : JsonSerializer.Serialize(obj, _camelCaseWithFallback);
}
