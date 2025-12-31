using BrowserHost.Features.Settings;

namespace BrowserHost.Tests.Fakes.StateManagers;

internal class FakeSettingsStateManager : SettingsStateManager
{
    public SettingsDataV1 RestoreSettingsValue { get; set; } = new(null, [], false);

    public List<SettingsDataV1> SaveInvocations { get; } = [];

    public SettingsDataV1? SaveReturnValue { get; set; }

    public override SettingsDataV1 RestoreSettingsFromDisk() =>
        RestoreSettingsValue;

    public override SettingsDataV1 SaveSettings(SettingsDataV1 settings)
    {
        SaveInvocations.Add(settings);
        return SaveReturnValue ?? settings;
    }
}
