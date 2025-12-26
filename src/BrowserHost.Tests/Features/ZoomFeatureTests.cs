using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.TypeConstructor;

namespace BrowserHost.Tests.Features;

public class ZoomFeatureTests
{
    [Fact]
    public void Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_zooms_in_by_2_points()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab, t => t.ZoomLevel = 0)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildZoomFeature();

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(0.2, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_down_with_Ctrl_pressed_zooms_out_by_2_points()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab, t => t.ZoomLevel = 0)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildZoomFeature();

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: -120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(-0.2, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_does_not_zoom_in_past_the_maximum_level()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab, t => t.ZoomLevel = 10)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildZoomFeature();

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(10, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_down_with_Ctrl_pressed_does_not_zoom_out_past_the_minimum_level()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab, t => t.ZoomLevel = -10)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildZoomFeature();

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: -120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(-10, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_without_Ctrl_pressed_is_not_handled_and_does_not_change_zoom()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab, t => t.ZoomLevel = 0)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.None)
            .BuildZoomFeature();

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.False(handled);
        Assert.False(tab.SetZoomCalled);
    }

    [Fact]
    public void Pressing_Ctrl_and_Delete_with_a_current_tab_resets_the_zoom_and_is_handled()
    {
        var feature = TestBrowserContext.CreateFeature
            .WithCurrentTab(out var tab)
            .ConfigureContext(ctx => ctx.CurrentKeyboardModifiers = ModifierKeys.Control)
            .BuildZoomFeature();

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Delete));

        Assert.True(handled);
        Assert.True(tab.ResetZoomCalled);
    }
}
