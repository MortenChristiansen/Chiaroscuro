using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

public record ActionDialogShownEvent();
public record ActionDialogDismissedEvent();
public record CommandExecutedEvent(string Command, bool Ctrl);
public record ActionDialogValueChangedEvent(string Value);

public class ActionDialogBackendApi(PubSub pubSub) : BackendApi
{
    public void Execute(string command, bool ctrl) =>
        pubSub.Publish(new CommandExecutedEvent(command, ctrl));

    public void DismissActionDialog() =>
        pubSub.Publish(new ActionDialogDismissedEvent());

    public void NotifyValueChanged(string value) =>
        pubSub.Publish(new ActionDialogValueChangedEvent(value));

    public string GetActionType(string command) =>
        ActionDialogFeature.GetActionType(command).ToString();
}
