using BrowserHost.Features.Zoom;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BrowserHost.Tests.Features;

public class ZoomFeatureTests
{
    private static MouseWheelEventArgs CreateMouseWheelEventArgs(int delta)
    {
        var win32MouseDeviceType = typeof(MouseDevice).Assembly.GetType("System.Windows.Input.Win32MouseDevice", throwOnError: true)!;
        var mouseDevice = (MouseDevice)RuntimeHelpers.GetUninitializedObject(win32MouseDeviceType);
        return new MouseWheelEventArgs(mouseDevice, timestamp: 0, delta);
    }

    private static KeyEventArgs CreateKeyEventArgs(Key key)
    {
        var e = (KeyEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(KeyEventArgs));

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(KeyEventArgs).GetField("_key", flags)!.SetValue(e, key);
        typeof(KeyEventArgs).GetField("_realKey", flags)!.SetValue(e, key);

        return e;
    }

    private sealed class TestableZoomFeature : ZoomFeature
    {
        public TestableZoomFeature() : base((MainWindow)RuntimeHelpers.GetUninitializedObject(typeof(MainWindow)))
        {
        }

        public ModifierKeys Modifiers { get; set; }
        public bool HasTab { get; set; } = true;
        public double? CurrentZoom { get; set; }

        public bool SetZoomCalled { get; private set; }
        public double? SetZoomTo { get; private set; }
        public bool ResetZoomCalled { get; private set; }

        protected override ModifierKeys CurrentKeyboardModifiers => Modifiers;

        protected override Task<double?> GetCurrentZoomLevelAsync() => Task.FromResult(CurrentZoom);
        protected override void SetCurrentZoomLevel(double level)
        {
            SetZoomCalled = true;
            SetZoomTo = level;
            CurrentZoom = level;
        }

        protected override void ResetCurrentZoomLevel()
        {
            ResetZoomCalled = true;
        }

        protected override bool HasCurrentTab => HasTab;
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_zooms_in_by_2_points()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.Control,
            CurrentZoom = 0,
            HasTab = true,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.True(handled);
        Assert.True(feature.SetZoomCalled);
        Assert.Equal(0.2, feature.SetZoomTo);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_down_with_Ctrl_pressed_zooms_out_by_2_points()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.Control,
            CurrentZoom = 0,
            HasTab = true,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: -120));

        Assert.True(handled);
        Assert.True(feature.SetZoomCalled);
        Assert.Equal(-0.2, feature.SetZoomTo);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_up_with_Ctrl_pressed_does_not_zoom_in_past_the_maximum_level()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.Control,
            CurrentZoom = 10,
            HasTab = true,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.True(handled);
        Assert.True(feature.SetZoomCalled);
        Assert.Equal(10, feature.SetZoomTo);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_down_with_Ctrl_pressed_does_not_zoom_out_past_the_minimum_level()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.Control,
            CurrentZoom = -10,
            HasTab = true,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: -120));

        Assert.True(handled);
        Assert.True(feature.SetZoomCalled);
        Assert.Equal(-10, feature.SetZoomTo);
    }

    [Fact]
    public void Scrolling_the_mouse_wheel_without_Ctrl_pressed_is_not_handled_and_does_not_change_zoom()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.None,
            CurrentZoom = 0,
            HasTab = true,
        };

        var handled = feature.HandleOnPreviewMouseWheel(CreateMouseWheelEventArgs(delta: 120));

        Assert.False(handled);
        Assert.False(feature.SetZoomCalled);
    }

    [Fact]
    public void Pressing_Ctrl_and_Delete_with_a_current_tab_resets_the_zoom_and_is_handled()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.Control,
            HasTab = true,
        };

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Delete));

        Assert.True(handled);
        Assert.True(feature.ResetZoomCalled);
    }

    [Fact]
    public void Pressing_Ctrl_and_Delete_without_a_current_tab_is_handled_but_does_not_reset_zoom()
    {
        var feature = new TestableZoomFeature
        {
            Modifiers = ModifierKeys.Control,
            HasTab = false,
        };

        var handled = feature.HandleOnPreviewKeyDown(CreateKeyEventArgs(Key.Delete));

        Assert.True(handled);
        Assert.False(feature.ResetZoomCalled);
    }
}
