using System.Windows;
using System.Windows.Media;
using BrowserHost.Interop;

namespace BrowserHost.XamlUtilities;

public static class VisualDpiUtil
{
    public static Point GetCursorPositionInDips(Visual referenceVisual)
    {
        if (!MonitorInterop.GetCursorPos(out var pt))
            return new Point(0, 0);

        var pixelPoint = new Point(pt.X, pt.Y);
        var source = PresentationSource.FromVisual(referenceVisual);
        if (source?.CompositionTarget is null)
            return pixelPoint; // Fallback; may be off on high-DPI

        var transformFromDevice = source.CompositionTarget.TransformFromDevice;
        return transformFromDevice.Transform(pixelPoint);
    }

    public static Vector GetDpiAwareOffset(Visual referenceVisual, double pixelX, double pixelY)
    {
        var source = PresentationSource.FromVisual(referenceVisual);
        if (source?.CompositionTarget is null)
            return new Vector(pixelX, pixelY); // Fallback; may be off on high-DPI

        var transformFromDevice = source.CompositionTarget.TransformFromDevice;
        var dip = transformFromDevice.Transform(new Point(pixelX, pixelY));
        return new Vector(dip.X, dip.Y);
    }
}
