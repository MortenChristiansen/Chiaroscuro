using BrowserHost.Features.TabPalette;

namespace BrowserHost.Tests.Fakes.WindowOperations;

internal class FakeTabPaletteWindowOperations : TabPaletteWindowOperations
{
    public int ShowCallCount { get; private set; }
    public int HideCallCount { get; private set; }
    public int FocusCallCount { get; private set; }

    public double ActualWidthValue { get; set; }

    private Action? _resizeCompleted;

    public override void RegisterTabPaletteResizeCompletedHandler(Action handler) => _resizeCompleted += handler;

    public void RaiseResizeCompleted() => _resizeCompleted?.Invoke();

    public override double TabPaletteActualWidth => ActualWidthValue;

    public override void ShowTabPalette() => ShowCallCount++;

    public override void HideTabPalette() => HideCallCount++;

    public override void FocusTabPalette() => FocusCallCount++;
}
