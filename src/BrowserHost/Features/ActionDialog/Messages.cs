using BrowserHost.Tab;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

public record ShowActionDialogCommand() : ICommand;
public record ActionDialogShownEvent() : IEvent;

public record DismissActionDialogCommand() : ICommand;
public record ActionDialogDismissedEvent() : IEvent;

public record ExecuteCommandCommand(string Command, bool Ctrl) : ICommand;
public record CommandExecutedEvent(string Command, bool Ctrl) : IEvent;

public record ChangeActionDialogValueCommand(string Value) : ICommand;
public record ActionDialogValueChangedEvent(string Value) : IEvent;

public record StartNavigationCommand(string Address, bool UseCurrentTab, bool SaveInHistory, bool ActivateTab, ITabBrowser? ReuseTabBrowser = null) : Utilities.ICommand;
public record NavigationStartedEvent(string Address, bool UseCurrentTab, bool SaveInHistory, bool ActivateTab, ITabBrowser? ReuseTabBrowser = null) : IEvent;
