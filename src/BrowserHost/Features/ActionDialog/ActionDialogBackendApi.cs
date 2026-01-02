using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

public record ShowActionDialogCommand() : ICommand;
public record DismissActionDialogCommand() : ICommand;
public record ExecuteCommandCommand(string Command, bool Ctrl) : ICommand;
public record ChangeActionDialogValueCommand(string Value) : ICommand;

public record ActionDialogShownEvent() : IEvent;
public record ActionDialogDismissedEvent() : IEvent;
public record CommandExecutedEvent(string Command, bool Ctrl) : IEvent;
public record ActionDialogValueChangedEvent(string Value) : IEvent;

public class ActionDialogBackendApi(PubSub pubSub) : BackendApi
{
    public void Execute(string command, bool ctrl) =>
        pubSub.Send(new ExecuteCommandCommand(command, ctrl));

    public void DismissActionDialog() =>
        pubSub.Send(new DismissActionDialogCommand());

    public void NotifyValueChanged(string value) =>
        pubSub.Send(new ChangeActionDialogValueCommand(value));

    public string GetActionType(string command) =>
        ActionDialogFeature.GetActionType(command).ToString();
}
