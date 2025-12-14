using System;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserHost.Features.CustomWindowChrome;

public record DomainTrustRating(string Source, double Score, int Stars, long FetchedAt);

public static partial class DomainTrustRatingProvider
{
    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.All
        });

        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

        return client;
    }

    public static async Task<DomainTrustRating?> LookupTrustpilotAsync(string domain)
    {
        var url = $"https://www.trustpilot.com/review/{Uri.EscapeDataString(domain)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        try
        {
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var payload = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return null;

            var score = ExtractTrustpilotScore(payload);
            if (score == null)
                return null;

            var clamped = ClampScore(score.Value);
            var stars = RoundToStars(clamped);
            var fetchedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return new DomainTrustRating("trustpilot", clamped, stars, fetchedAt);
        }
        catch
        {
            return null;
        }
    }

    private static double? ExtractTrustpilotScore(string page)
    {
        if (string.IsNullOrWhiteSpace(page))
            return null;

        var normalized = WhitespaceRegex().Replace(page, " ");
        var titleMatch = RateRegex().Match(normalized);

        if (titleMatch.Success)
        {
            var parsed = ParseScore(titleMatch.Groups[1].Value);
            if (parsed != null)
                return parsed;
        }

        var aggregateMatch = RateRegex2().Match(page);
        if (aggregateMatch.Success)
        {
            var parsed = ParseScore(aggregateMatch.Groups[1].Value);
            if (parsed != null)
                return parsed;
        }

        var trustScoreMatch = ScoreRegex().Match(page);
        if (trustScoreMatch.Success)
        {
            var parsed = ParseScore(trustScoreMatch.Groups[1].Value);
            if (parsed != null)
                return parsed;
        }

        return null;
    }

    private static double? ParseScore(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Trim().Replace(',', '.');
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
            return null;

        // Score of 0 indicates no reviews.
        if (score <= 0)
            return null;

        return score;
    }

    private static double ClampScore(double value) => Math.Min(5, Math.Max(0, value));

    private static int RoundToStars(double value)
    {
        var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
        return Math.Min(5, Math.Max(1, rounded));
    }

    [GeneratedRegex("is rated \"[^\"]+\" with\\s+([0-9]+(?:[\\.,][0-9]+)?)\\s*/\\s*5", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RateRegex();
    [GeneratedRegex("\"@type\"\\s*:\\s*\"AggregateRating\"[\\s\\S]*?\"ratingValue\"\\s*:\\s*\"?([0-9]+(?:[\\.,][0-9]+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RateRegex2();
    [GeneratedRegex("TrustScore[^0-9]*([0-5](?:[\\.,][0-9]+)?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ScoreRegex();
    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
