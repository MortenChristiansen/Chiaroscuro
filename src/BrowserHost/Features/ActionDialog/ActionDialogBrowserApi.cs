using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

public record ActionDialogShownEvent();
public record ActionDialogDismissedEvent();
public record CommandExecutedEvent(string Command, bool Ctrl);
public record ActionDialogValueChangedEvent(string Value);

public class ActionDialogBrowserApi : BrowserApi
{
    public void Execute(string command, bool ctrl) =>
        PubSub.Instance.Publish(new CommandExecutedEvent(command, ctrl));

    public void DismissActionDialog() =>
        PubSub.Instance.Publish(new ActionDialogDismissedEvent());

    public void NotifyValueChanged(string value) =>
        PubSub.Instance.Publish(new ActionDialogValueChangedEvent(value));

    public string GetActionType(string command) =>
        ActionDialogFeature.GetActionType(command).ToString();
}
