using System;
using System.Windows;
using System.Windows.Media;

namespace BrowserHost.XamlUtilities;

/// <summary>
/// Base class for windows that overlay a specific element in an owner window.
/// Handles automatic positioning and sizing to match the target element.
/// </summary>
public abstract class OverlayWindow : Window
{
    private Window? _ownerWindow;
    private FrameworkElement? _targetElement;

    /// <summary>
    /// Gets or sets the target element that this window should overlay.
    /// The window will automatically position and resize to match this element.
    /// </summary>
    protected FrameworkElement? TargetElement
    {
        get => _targetElement;
        set
        {
            if (_targetElement != null)
            {
                _targetElement.SizeChanged -= OnTargetElementSizeOrLayoutChanged;
                _targetElement.LayoutUpdated -= OnTargetElementSizeOrLayoutChanged;
            }

            _targetElement = value;

            if (_targetElement != null)
            {
                _targetElement.SizeChanged += OnTargetElementSizeOrLayoutChanged;
                _targetElement.LayoutUpdated += OnTargetElementSizeOrLayoutChanged;
            }

            UpdateOverlayBounds();
        }
    }

    /// <summary>
    /// Gets or sets the owner window that this overlay is attached to.
    /// </summary>
    protected Window? OwnerWindow
    {
        get => _ownerWindow;
        set
        {
            if (_ownerWindow != null)
            {
                _ownerWindow.LocationChanged -= OnOwnerWindowLocationOrSizeChanged;
                _ownerWindow.SizeChanged -= OnOwnerWindowLocationOrSizeChanged;
                _ownerWindow.StateChanged -= OnOwnerWindowStateChanged;
            }

            _ownerWindow = value;

            if (_ownerWindow != null)
            {
                _ownerWindow.LocationChanged += OnOwnerWindowLocationOrSizeChanged;
                _ownerWindow.SizeChanged += OnOwnerWindowLocationOrSizeChanged;
                _ownerWindow.StateChanged += OnOwnerWindowStateChanged;
            }

            UpdateOverlayBounds();
        }
    }

    protected OverlayWindow()
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Manual;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        UpdateOverlayBounds();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        DetachHandlers();
    }

    private void OnOwnerWindowStateChanged(object? sender, EventArgs e)
    {
        if (_ownerWindow == null) return;

        if (_ownerWindow.WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Minimized;
        }
        else if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        OnOwnerWindowLocationOrSizeChanged(sender, e);
    }

    private void OnOwnerWindowLocationOrSizeChanged(object? sender, EventArgs e)
    {
        UpdateOverlayBounds();
    }

    private void OnTargetElementSizeOrLayoutChanged(object? sender, EventArgs e)
    {
        UpdateOverlayBounds();
    }

    /// <summary>
    /// Updates the window's position and size to match the target element.
    /// Can be overridden to customize positioning behavior.
    /// </summary>
    protected virtual void UpdateOverlayBounds()
    {
        if (_ownerWindow == null || _targetElement == null) return;
        if (!IsLoaded) return;

        // Get position of target element relative to owner window (DIPs)
        var topLeft = _targetElement.TranslatePoint(new Point(0, 0), _ownerWindow);
        var width = _targetElement.ActualWidth;
        var height = _targetElement.ActualHeight;

        if (width <= 0 || height <= 0) return;

        Left = _ownerWindow.Left + topLeft.X;
        Top = _ownerWindow.Top + topLeft.Y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Detaches all event handlers from the owner window and target element.
    /// Called automatically when the window is closed.
    /// </summary>
    protected void DetachHandlers()
    {
        if (_targetElement != null)
        {
            _targetElement.SizeChanged -= OnTargetElementSizeOrLayoutChanged;
            _targetElement.LayoutUpdated -= OnTargetElementSizeOrLayoutChanged;
            _targetElement = null;
        }

        if (_ownerWindow != null)
        {
            _ownerWindow.LocationChanged -= OnOwnerWindowLocationOrSizeChanged;
            _ownerWindow.SizeChanged -= OnOwnerWindowLocationOrSizeChanged;
            _ownerWindow.StateChanged -= OnOwnerWindowStateChanged;
            _ownerWindow = null;
        }
    }
}
