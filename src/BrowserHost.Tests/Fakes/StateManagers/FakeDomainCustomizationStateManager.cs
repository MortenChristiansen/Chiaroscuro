using BrowserHost.Features.TabPalette.DomainCustomization;

namespace BrowserHost.Tests.Fakes.StateManagers;

internal sealed class FakeDomainCustomizationStateManager : DomainCustomizationStateManager
{
    private readonly Dictionary<string, DomainCustomizationDataV1> _customizations = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _cssByDomain = new(StringComparer.OrdinalIgnoreCase);

    public override DomainCustomizationDataV1 GetCustomization(string domain)
    {
        if (_customizations.TryGetValue(domain, out var customization))
            return customization;

        var hasCustomCss = _cssByDomain.ContainsKey(domain);
        var created = new DomainCustomizationDataV1(domain, CssEnabled: hasCustomCss, HasCustomCss: hasCustomCss);
        _customizations[domain] = created;
        return created;
    }

    public override DomainCustomizationDataV1? SaveCustomization(DomainCustomizationDataV1 customization)
    {
        var hasCustomCss = _cssByDomain.ContainsKey(customization.Domain);
        var updated = customization with { HasCustomCss = hasCustomCss };
        _customizations[customization.Domain] = updated;
        return updated;
    }

    public override string? GetCustomCss(string domain) =>
        _cssByDomain.TryGetValue(domain, out var css) ? css : null;

    public override void RefreshCacheForDomain(string domain) =>
        _customizations.Remove(domain);

    public override void RemoveCustomCss(string domain)
    {
        _cssByDomain.Remove(domain);

        if (_customizations.TryGetValue(domain, out var existing))
        {
            _customizations[domain] = existing with { HasCustomCss = false };
        }
    }

    public override bool EnsureCustomCssExistsAndOpenInEditor(string domain)
    {
        // In tests we don't open editors; we just ensure "has css" state exists.
        if (!_cssByDomain.ContainsKey(domain))
        {
            _cssByDomain[domain] = $"/* Custom CSS for {domain} */\n\n";
        }

        if (_customizations.TryGetValue(domain, out var existing))
        {
            _customizations[domain] = existing with { HasCustomCss = true };
        }

        return true;
    }

    public override IDisposable WatchCustomCss(string domain, Action<CustomCssWatchEventKind> onEvent) =>
        NoopWatchSubscription.Instance;

    public void SetCustomCss(string domain, string css)
    {
        _cssByDomain[domain] = css;

        if (_customizations.TryGetValue(domain, out var existing))
        {
            _customizations[domain] = existing with { HasCustomCss = true };
        }
    }

    private sealed class NoopWatchSubscription : IDisposable
    {
        public static NoopWatchSubscription Instance { get; } = new();
        public void Dispose() { }
    }
}
