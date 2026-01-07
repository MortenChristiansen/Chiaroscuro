using System;

namespace BrowserHost.Features.TabPalette;

public class TabPaletteWindowOperations
{
    private MainWindow _window = null!;

    private event Action? _resizeCompleted;

    protected TabPaletteWindowOperations() { }

    public TabPaletteWindowOperations(MainWindow window)
    {
        _window = window;
        _window.TabPaletteGridSplitter.DragCompleted += (_, __) => _resizeCompleted?.Invoke();
    }

    public virtual void RegisterTabPaletteResizeCompletedHandler(Action handler) => _resizeCompleted += handler;

    public virtual double TabPaletteActualWidth => _window.TabPaletteColumn.ActualWidth;

    public virtual void ShowTabPalette() => _window.ShowTabPalette();

    public virtual void HideTabPalette() => _window.HideTabPalette();

    public virtual void FocusTabPalette() => _window.TabPaletteBrowserControl.Focus();
}
