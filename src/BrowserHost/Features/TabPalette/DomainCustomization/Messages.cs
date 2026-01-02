using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette.DomainCustomization;

public record ChangeDomainCustomizationCommand(string Domain, bool CssEnabled) : ICommand;
public record DomainCustomizationChangedEvent(string Domain, bool CssEnabled) : IEvent;

public record EditDomainCssCommand(string Domain) : ICommand;
public record DomainCssEditRequestedEvent(string Domain) : IEvent;

public record RemoveDomainCustomCssCommand(string Domain) : ICommand;
public record DomainCustomCssRemovedEvent(string Domain) : IEvent;
