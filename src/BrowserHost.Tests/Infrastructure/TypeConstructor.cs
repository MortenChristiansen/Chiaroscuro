using BrowserHost.Tab;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Windows.Input;

namespace BrowserHost.Tests.Infrastructure;

internal static class TypeConstructor
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

    public static TabBrowser CreateTabBrowser(string? tabId = null)
    {
#pragma warning disable SYSLIB0050 // GetUninitializedObject is acceptable for test-only construction.
        var tab = (TabBrowser)FormatterServices.GetUninitializedObject(typeof(TabBrowser));
#pragma warning restore SYSLIB0050

        var browserField = typeof(TabBrowser).GetField("_browser", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(browserField);
        tabId ??= Guid.NewGuid().ToString();
        browserField.SetValue(tab, new FakeTabWebBrowser(tabId));
        return tab;
    }
}
