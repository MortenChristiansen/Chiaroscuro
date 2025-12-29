using BrowserHost.Features.TabPalette.TabCustomization;

namespace BrowserHost.Tests.Fakes.StateManagers;

internal class FakeTabCustomizationStateManager : TabCustomizationStateManager
{
    private readonly Dictionary<string, TabCustomizationDataV1> _customizations = [];

    public List<string> DeletedTabIds { get; } = [];

    public override TabCustomizationDataV1 GetCustomization(string tabId)
    {
        if (_customizations.TryGetValue(tabId, out var customization))
            return customization;

        return new TabCustomizationDataV1(tabId, CustomTitle: null, DisableFixedAddress: false);
    }

    public override IReadOnlyCollection<TabCustomizationDataV1> GetAllCustomizations() =>
        [.. _customizations.Values];

    public override TabCustomizationDataV1? SaveCustomization(string tabId, Func<TabCustomizationDataV1, TabCustomizationDataV1> updateData)
    {
        var defaultCustomization = new TabCustomizationDataV1(tabId, CustomTitle: null, DisableFixedAddress: false);

        if (_customizations.TryGetValue(tabId, out var existing))
        {
            var updatedExisting = updateData(existing);
            if (updatedExisting == existing)
                return existing;

            if (updatedExisting == defaultCustomization)
            {
                DeleteCustomization(tabId);
                return null;
            }

            _customizations[tabId] = updatedExisting;
            return updatedExisting;
        }

        var updatedNew = updateData(defaultCustomization);
        if (updatedNew == defaultCustomization)
            return null;

        _customizations[tabId] = updatedNew;
        return updatedNew;
    }

    public override void DeleteCustomization(string tabId)
    {
        _customizations.Remove(tabId);
        DeletedTabIds.Add(tabId);
    }
}
