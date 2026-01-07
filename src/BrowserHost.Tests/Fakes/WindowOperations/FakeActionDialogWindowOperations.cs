using BrowserHost.Features.ActionDialog;

namespace BrowserHost.Tests.Fakes.WindowOperations;

internal class FakeActionDialogWindowOperations : ActionDialogWindowOperations
{
    public bool IsVisibleValue { get; set; }

    public int ShowCallCount { get; private set; }
    public int HideCallCount { get; private set; }

    public override bool ActionDialogIsVisible => IsVisibleValue;

    public override void ShowActionDialog()
    {
        ShowCallCount++;
        IsVisibleValue = true;
    }

    public override void HideActionDialog()
    {
        HideCallCount++;
        IsVisibleValue = false;
    }
}
