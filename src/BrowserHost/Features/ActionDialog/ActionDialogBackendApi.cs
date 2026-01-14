using BrowserHost.CefInfrastructure;
using BrowserHost.Utilities;

namespace BrowserHost.Features.ActionDialog;

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
