using System.Windows;
using System.Windows.Controls;

namespace BrowserHost.Features.AppState;

public class AppStateFeature(MainWindow window) : Feature(window)
{
    private ColumnDefinition? _actionContextColumn;

    public override void Configure()
    {
        _actionContextColumn = Window.ActionContextColumn;

        Window.Loaded += (_, __) => ApplyInitialLayout();

        Window.ActionContextGridSplitter.DragCompleted += (_, __) =>
        {
            if (_actionContextColumn != null)
            {
                var width = _actionContextColumn.ActualWidth;
                AppStateStateManager.SaveActionContextWidth(width);
            }
        };

        Window.TabPaletteGridSplitter.DragCompleted += (_, __) =>
        {
            var tabPaletteCol = Window.TabPaletteColumn;
            if (tabPaletteCol.ActualWidth > 0)
            {
                AppStateStateManager.SaveTabPaletteWidth(tabPaletteCol.ActualWidth);
            }
        };
    }

    private void ApplyInitialLayout()
    {
        var layout = AppStateStateManager.RestoreAppStateFromDisk();

        if (_actionContextColumn != null && layout.ActionContextWidth > 0)
            _actionContextColumn.Width = new GridLength(layout.ActionContextWidth);

        // TabPalette is restored when opened; keep collapsed until user shows it
    }
}
