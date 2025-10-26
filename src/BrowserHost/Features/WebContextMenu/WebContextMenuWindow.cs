using BrowserHost.XamlUtilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrowserHost.Features.WebContextMenu;

public class WebContextMenuWindow : OverlayWindow
{
    private readonly Border _menuContainer;
    private const int MenuWidth = 250;
    private const int MenuCornerRadius = 8;

    public WebContextMenuWindow(Window owner, double x, double y)
    {
        Owner = owner;
        // Ensure OverlayWindow can track and size relative to the owner window
        OwnerWindow = owner;
        Left = x;
        Top = y;
        // TODO: Fixes dismiss logic activating other applications, though we need it to be activated eventually
        ShowActivated = false; // Do not activate/focus this overlay window
        Focusable = false; // Prevent keyboard focus

        // Create the menu UI
        _menuContainer = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(MenuCornerRadius),
            Padding = new Thickness(8),
            Width = MenuWidth,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            // Make the menu visible. If a fade-in is desired, animate this value explicitly.
            Opacity = 1
        };

        var contentStack = new StackPanel();

        var noContentText = new TextBlock
        {
            Text = "No content",
            Foreground = Brushes.White,
            Margin = new Thickness(4)
        };
        contentStack.Children.Add(noContentText);

        _menuContainer.Child = contentStack;

        var rootGrid = new Grid
        {
            Background = Brushes.Transparent
        };
        rootGrid.Children.Add(_menuContainer);
        Content = rootGrid;
    }
}
