using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BrowserHost.Tests.Infrastructure;

internal static class EventArgHelpers
{
    public static MouseWheelEventArgs CreateMouseWheelEventArgs(int delta)
    {
        var win32MouseDeviceType = typeof(MouseDevice).Assembly.GetType("System.Windows.Input.Win32MouseDevice", throwOnError: true)!;
        var mouseDevice = (MouseDevice)RuntimeHelpers.GetUninitializedObject(win32MouseDeviceType);
        return new MouseWheelEventArgs(mouseDevice, timestamp: 0, delta);
    }

    public static KeyEventArgs CreateKeyEventArgs(Key key)
    {
        var e = (KeyEventArgs)RuntimeHelpers.GetUninitializedObject(typeof(KeyEventArgs));

        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(KeyEventArgs).GetField("_key", flags)!.SetValue(e, key);
        typeof(KeyEventArgs).GetField("_realKey", flags)!.SetValue(e, key);

        return e;
    }
}
