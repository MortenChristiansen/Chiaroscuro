using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BrowserHost.Features.Notifications;

public class NotificationPermissionDialog : Window
{
    public NotificationPermissionDialog(string origin)
    {
        Title = "Notification Permission";
        Width = 450;
        Height = 200;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        // Make window transparent and draw our own rounded border
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        // Remove window-level border to avoid square outline
        BorderBrush = null;
        BorderThickness = new Thickness(0);

        CreateContent(origin);
    }

    private void CreateContent(string origin)
    {
        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Content area
        var contentPanel = new StackPanel();
        contentPanel.Margin = new Thickness(20);
        contentPanel.VerticalAlignment = VerticalAlignment.Center;

        // Icon and text container
        var headerPanel = new StackPanel();
        headerPanel.Orientation = Orientation.Horizontal;
        headerPanel.Margin = new Thickness(0, 0, 0, 15);

        // Notification icon (simple circle)
        var iconBorder = new Border();
        iconBorder.Width = 32;
        iconBorder.Height = 32;
        iconBorder.CornerRadius = new CornerRadius(16);
        iconBorder.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        iconBorder.Margin = new Thickness(0, 0, 15, 0);

        var iconText = new TextBlock();
        iconText.Text = "🔔";
        iconText.FontSize = 16;
        iconText.HorizontalAlignment = HorizontalAlignment.Center;
        iconText.VerticalAlignment = VerticalAlignment.Center;
        iconBorder.Child = iconText;

        var titleText = new TextBlock();
        titleText.Text = "Notification Permission Request";
        titleText.FontSize = 16;
        titleText.FontWeight = FontWeights.Bold;
        titleText.Foreground = Brushes.White;
        titleText.VerticalAlignment = VerticalAlignment.Center;

        headerPanel.Children.Add(iconBorder);
        headerPanel.Children.Add(titleText);

        // Message text
        var messageText = new TextBlock();
        messageText.Text = $"The website {origin} wants to show notifications.";
        messageText.FontSize = 14;
        messageText.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        messageText.TextWrapping = TextWrapping.Wrap;
        messageText.Margin = new Thickness(0, 0, 0, 10);

        contentPanel.Children.Add(headerPanel);
        contentPanel.Children.Add(messageText);

        // Button area
        var buttonPanel = new StackPanel();
        buttonPanel.Orientation = Orientation.Horizontal;
        buttonPanel.HorizontalAlignment = HorizontalAlignment.Right;
        buttonPanel.Margin = new Thickness(20, 0, 20, 20);
        // Match window background (no darker strip behind buttons)
        buttonPanel.Background = Brushes.Transparent;

        var denyButton = CreateStyledButton("Deny", false);
        var allowButton = CreateStyledButton("Allow", true);

        buttonPanel.Children.Add(denyButton);
        buttonPanel.Children.Add(allowButton);

        Grid.SetRow(contentPanel, 0);
        Grid.SetRow(buttonPanel, 1);

        mainGrid.Children.Add(contentPanel);
        mainGrid.Children.Add(buttonPanel);

        // Wrap content in a rounded border to create rounded window corners
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

        // Set focus to deny button by default (safer choice)
        Loaded += (s, e) => denyButton.Focus();
    }

    private Button CreateStyledButton(string text, bool isAllow)
    {
        var button = new Button();
        button.Content = text;
        button.Width = 80;
        button.Height = 32;
        button.Margin = new Thickness(10, 0, 0, 0);
        button.FontSize = 14;

        if (isAllow)
        {
            button.Background = new SolidColorBrush(Color.FromRgb(0, 120, 215));
            button.Foreground = Brushes.White;
            button.Click += (s, e) => { DialogResult = true; };
            button.IsDefault = true;
        }
        else
        {
            button.Background = new SolidColorBrush(Color.FromRgb(68, 68, 68));
            button.Foreground = Brushes.White;
            button.Click += (s, e) => { DialogResult = false; };
            button.IsCancel = true;
        }

        button.BorderThickness = new Thickness(0);
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

        // Add hover effect
        var hoverTrigger = new Trigger();
        hoverTrigger.Property = Button.IsMouseOverProperty;
        hoverTrigger.Value = true;

        var hoverSetter = new Setter();
        hoverSetter.TargetName = "border";
        hoverSetter.Property = Border.BackgroundProperty;
        hoverSetter.Value = isAllow
            ? new SolidColorBrush(Color.FromRgb(16, 110, 190))
            : new SolidColorBrush(Color.FromRgb(80, 80, 80));

        hoverTrigger.Setters.Add(hoverSetter);
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    public static bool ShowDialog(Window owner, string origin)
    {
        var dialog = new NotificationPermissionDialog(origin);
        dialog.Owner = owner;
        return dialog.ShowDialog() == true;
    }
}
