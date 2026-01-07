using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features.ActionContext;

public class ActionContextFeatureTest
{
    [Fact]
    public void Pressing_Ctrl_and_S_toggles_the_action_context_and_is_handled()
    {
        var feature = CreateFeature
            .CaptureContext(out var context)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildActionContextFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.S));

        Assert.True(handled);
        Assert.Equal(1, context.ActionContextWindowOperations.ToggleVisibilityCallCount);
    }
}
