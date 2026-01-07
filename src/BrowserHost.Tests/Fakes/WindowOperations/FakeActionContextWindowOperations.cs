using BrowserHost.Features.ActionContext;
using System;

namespace BrowserHost.Tests.Fakes.WindowOperations;

internal class FakeActionContextWindowOperations : ActionContextWindowOperations
{
    public int ToggleVisibilityCallCount { get; private set; }

    public double ActualWidthValue { get; set; }

    public double WidthSetTo { get; private set; }

    private Action? _resizeCompleted;

    public override void RegisterActionContextResizeCompletedHandler(Action handler) => _resizeCompleted += handler;

    public void RaiseResizeCompleted() => _resizeCompleted?.Invoke();

    public override double ActionContextActualWidth => ActualWidthValue;

    public override void SetActionContextWidth(double width) => WidthSetTo = width;

    public override void ToggleActionContextVisibility() => ToggleVisibilityCallCount++;
}
