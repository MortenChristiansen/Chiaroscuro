using BrowserHost.Features.Zoom;
using BrowserHost.Tab;
using System.Windows.Input;
using static BrowserHost.Tests.Infrastructure.EventArgHelpers;

namespace BrowserHost.Tests.Features;

public class ZoomFeatureTests
{
    private sealed class TestableZoomFeature(ITabBrowser? tabBrowser) : ZoomFeature(null!, new TestBrowserContext(tabBrowser))
    {
        public ModifierKeys Modifiers { get; set; }
        protected override ModifierKeys CurrentKeyboardModifiers => Modifiers;
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_zooms_in_by_2_points()
    {
        var tab = new TestTabBrowser
        {
            ZoomLevel = 0,
        };
        var feature = new TestableZoomFeature(tab)
        {
            Modifiers = ModifierKeys.Control,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(0.2, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_down_with_Ctrl_pressed_zooms_out_by_2_points()
    {
        var tab = new TestTabBrowser
        {
            ZoomLevel = 0,
        };
        var feature = new TestableZoomFeature(tab)
        {
            Modifiers = ModifierKeys.Control,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: -120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(-0.2, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_does_not_zoom_in_past_the_maximum_level()
    {
        var tab = new TestTabBrowser
        {
            ZoomLevel = 10,
        };
        var feature = new TestableZoomFeature(tab)
        {
            Modifiers = ModifierKeys.Control,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(10, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_down_with_Ctrl_pressed_does_not_zoom_out_past_the_minimum_level()
    {
        var tab = new TestTabBrowser
        {
            ZoomLevel = -10,
        };
        var feature = new TestableZoomFeature(tab)
        {
            Modifiers = ModifierKeys.Control,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: -120));

        Assert.True(handled);
        Assert.True(tab.SetZoomCalled);
        Assert.Equal(-10, tab.ZoomLevel);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_without_Ctrl_pressed_is_not_handled_and_does_not_change_zoom()
    {
        var tab = new TestTabBrowser
        {
            ZoomLevel = 0,
        };
        var feature = new TestableZoomFeature(tab)
        {
            Modifiers = ModifierKeys.None,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.False(handled);
        Assert.False(tab.SetZoomCalled);
    }

    [Fact]
    public void Pressing_Ctrl_and_Delete_with_a_current_tab_resets_the_zoom_and_is_handled()
    {
        var tab = new TestTabBrowser
        {
        };
        var feature = new TestableZoomFeature(tab)
        {
            Modifiers = ModifierKeys.Control,
        };

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Delete));

        Assert.True(handled);
        Assert.True(tab.ResetZoomCalled);
    }
}
