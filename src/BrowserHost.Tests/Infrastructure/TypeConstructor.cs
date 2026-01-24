using BrowserHost.Tab;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace BrowserHost.Tests.Infrastructure;

internal static class TypeConstructor
{
    public static T CreateUninitialized<T>() where T : class
    {
        return (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

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
        var keyField = typeof(KeyEventArgs).GetField("_key", flags) ?? throw new InvalidOperationException("KeyEventArgs._key field not found - internal structure may have changed");
        var realKeyField = typeof(KeyEventArgs).GetField("_realKey", flags) ?? throw new InvalidOperationException("KeyEventArgs._realKey field not found - internal structure may have changed");

        keyField.SetValue(e, key);
        realKeyField.SetValue(e, key);

        return e;
    }

    public static TabBrowser CreateTabBrowser(string? tabId = null)
    {
        var tab = (TabBrowser)RuntimeHelpers.GetUninitializedObject(typeof(TabBrowser));

        var browserField = typeof(TabBrowser).GetField("_browser", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("TabBrowser._browser field not found - internal structure may have changed");

        tabId ??= Guid.NewGuid().ToString();
        browserField.SetValue(tab, new FakeTabWebBrowser(tabId));
        return tab;
    }
}
