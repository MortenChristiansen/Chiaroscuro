using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.TabCustomization;

public record ChangeTabCustomTitleCommand(string TabId, string? CustomTitle) : ICommand;
public record TabCustomTitleChangedEvent(string TabId, string? CustomTitle) : IEvent;

public record ChangeTabDisableFixedAddressCommand(string TabId, bool IsDisabled) : ICommand;
public record TabDisableFixedAddressChangedEvent(string TabId, bool IsDisabled) : IEvent;
