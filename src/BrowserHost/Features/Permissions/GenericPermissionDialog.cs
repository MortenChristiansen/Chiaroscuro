using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrowserHost.Features.Permissions;

public class GenericPermissionDialog : Window
{
    public GenericPermissionDialog(string origin, string permissionDisplay)
    {
        Title = "Permission Request";
        Width = 450;
        Height = 200;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        BorderBrush = null;
        BorderThickness = new Thickness(0);
        CreateContent(origin, permissionDisplay);
    }

    private void CreateContent(string origin, string permissionDisplay)
    {
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var contentPanel = new StackPanel { Margin = new Thickness(20), VerticalAlignment = VerticalAlignment.Center };

        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };

        var iconBorder = new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(16),
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            Margin = new Thickness(0, 0, 15, 0)
        };

        var iconText = new TextBlock
        {
            Text = "🦄",
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconBorder.Child = iconText;

        var titleText = new TextBlock
        {
            Text = $"{permissionDisplay} Permission Request",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center
        };

        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(titleText);

        var messageText = new TextBlock
        {
            Text = $"The website {origin} requests permission: {permissionDisplay}.",
            FontSize = 14,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };

        contentPanel.Children.Add(headerPanel);
        contentPanel.Children.Add(messageText);

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 0, 20, 20),
            Background = Brushes.Transparent
        };

        var denyButton = CreateStyledButton("Deny", false);
        var allowButton = CreateStyledButton("Allow", true);

        buttonPanel.Children.Add(denyButton);
        buttonPanel.Children.Add(allowButton);

        Grid.SetRow(contentPanel, 0);
        Grid.SetRow(buttonPanel, 1);

        mainGrid.Children.Add(contentPanel);
        mainGrid.Children.Add(buttonPanel);

        var rootBorder = new Border
        {
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 74)),
            BorderThickness = new Thickness(1),
            SnapsToDevicePixels = true,
            Child = mainGrid
        };

        Content = rootBorder;
        Loaded += (_, _) => denyButton.Focus();
    }

    private Button CreateStyledButton(string text, bool isAllow)
    {
        var button = new Button
        {
            Content = text,
            Width = 80,
            Height = 32,
            Margin = new Thickness(10, 0, 0, 0),
            FontSize = 14,
            Background = isAllow ? new SolidColorBrush(Color.FromRgb(0, 120, 215)) : new SolidColorBrush(Color.FromRgb(68, 68, 68)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            IsDefault = isAllow,
            IsCancel = !isAllow
        };

        button.Click += (_, _) => { DialogResult = isAllow; };
        button.Template = CreateButtonTemplate(isAllow);
        return button;
    }

    private ControlTemplate CreateButtonTemplate(bool isAllow)
    {
        var template = new ControlTemplate(typeof(Button));

        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "border";
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

        border.AppendChild(contentPresenter);
        template.VisualTree = border;

        var hoverTrigger = new Trigger
        {
            Property = Button.IsMouseOverProperty,
            Value = true
        };
        var hoverSetter = new Setter
        {
            TargetName = "border",
            Property = Border.BackgroundProperty,
            Value = isAllow ? new SolidColorBrush(Color.FromRgb(16, 110, 190)) : new SolidColorBrush(Color.FromRgb(80, 80, 80))
        };
        hoverTrigger.Setters.Add(hoverSetter);
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    public static bool ShowDialog(Window owner, string origin, string permissionDisplay)
    {
        var dialog = new GenericPermissionDialog(origin, permissionDisplay);
        dialog.Owner = owner;
        return dialog.ShowDialog() == true;
    }
}
