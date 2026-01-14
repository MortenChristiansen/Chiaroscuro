using BrowserHost.Utilities;

namespace BrowserHost.Features.TabPalette;

public record RequestTabPaletteCommand() : Utilities.ICommand;
public record TabPaletteRequestedEvent() : IEvent;

public record DismissTabPaletteCommand() : Utilities.ICommand;
public record TabPaletteDismissedEvent() : IEvent;
