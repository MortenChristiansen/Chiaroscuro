using BrowserHost.Features.ActionDialog;
using BrowserHost.Interop;
using BrowserHost.Tab;
using BrowserHost.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace BrowserHost.Features.ActionContext.Tabs;

public class ChildBrowserWindow : Window
{
    private readonly TabBrowser _browser;
    private readonly string _parentTabId;
    private Window? _ownerWindow;
    private FrameworkElement? _targetElement; // MainWindow.WebContentBorder
    private const int _cornerRadiusDip = 8;
    private const int _overlayFadeDuration = 300;
    private readonly Border _contentHost;
    private readonly SolidColorBrush _overlayBrush;
    private readonly Grid _rootGrid;
    private readonly Grid _contentContainer;
    private readonly StackPanel _buttonsPanel;
    private bool _isClosing;
    private bool _contentAnimationStarted;

    // --- Static management of child windows per parent tab ---
    private static readonly Dictionary<string, List<ChildBrowserWindow>> _windowsByTab = [];
    private static readonly Lock _lock = new();

    static ChildBrowserWindow()
    {
        PubSub.Subscribe<TabActivatedEvent>(e =>
        {
            if (!string.IsNullOrEmpty(e.TabId)) ShowWindowsForTab(e.TabId);
            if (e.PreviousTab != null) HideWindowsForTab(e.PreviousTab.Id);
        });
        PubSub.Subscribe<TabDeactivatedEvent>(e =>
        {
            if (!string.IsNullOrEmpty(e.TabId)) HideWindowsForTab(e.TabId);
        });
        PubSub.Subscribe<TabClosedEvent>(e =>
        {
            if (!string.IsNullOrEmpty(e.Tab.Id)) CloseWindowsForTab(e.Tab.Id);
        });
    }

    public ChildBrowserWindow(string address, string parentTabId)
    {
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        AllowsTransparency = true;
        WindowStyle = WindowStyle.None;
        Owner = MainWindow.Instance;

        _browser = new TabBrowser($"{Guid.NewGuid()}", address, MainWindow.Instance.ActionContext, setManualAddress: false, favicon: null, isChildBrowser: true);
        _browser.PageLoadEnded += Browser_PageLoadEnded;

        _overlayBrush = new SolidColorBrush(Color.FromArgb(128, 180, 180, 200)) { Opacity = 0.0 };

        // Capture parent tab id and register this window
        _parentTabId = parentTabId;
        RegisterWindowForTab(_parentTabId, this);
        _rootGrid = new Grid { Background = _overlayBrush };

        // Centered content host - we size it relative to window size
        _contentHost = new Border
        {
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(8),
            SnapsToDevicePixels = true,
            Child = _browser,
            Opacity = 0,
            LayoutTransform = new ScaleTransform(0.5, 0.5)
        };

        // Overlay container that hosts content and floating buttons in the same centered layer
        _contentContainer = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = true
        };
        Panel.SetZIndex(_contentContainer, 1);
        _contentContainer.Children.Add(_contentHost);

        // Floating action buttons positioned to the right of the content (moved via transform)
        _buttonsPanel = new()
        {
            Orientation = Orientation.Vertical,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Opacity = 0,
            Visibility = Visibility.Collapsed,
            RenderTransform = new TranslateTransform()
        };
        Panel.SetZIndex(_buttonsPanel, 2);
        _contentContainer.Children.Add(_buttonsPanel);

        // Create buttons
        var closeBtn = CreateCircleButton(CreateIconX(), "Close");
        closeBtn.Click += (_, __) =>
        {
            BeginCloseWithFade();
            AnimateContentOut();
        };

        var convertBtn = CreateCircleButton(CreateIconPopOut(), "Open as regular tab");
        convertBtn.Margin = new Thickness(0, 8, 0, 0); // gap between buttons
        convertBtn.Click += (_, __) =>
        {
            var address = _browser.Address;
            // Trigger regular navigation (new tab)
            PubSub.Publish(new NavigationStartedEvent(address, UseCurrentTab: false, SaveInHistory: true));
            // Close this child window
            BeginCloseWithFade();
            AnimateContentOut();
        };

        _buttonsPanel.Children.Add(closeBtn);
        _buttonsPanel.Children.Add(convertBtn);

        _rootGrid.Children.Add(_contentContainer);
        Content = _rootGrid;

        Loaded += OnLoadedAttachToOwner;
        Loaded += (_, __) =>
        {
            TryHookDpiAndApplyRoundedCorners();
            ApplyRoundedWindowRegion();
        };
        Closed += (_, __) => { DetachOwnerHandlers(); UnregisterWindowForTab(_parentTabId, this); };
        SizeChanged += (_, __) => ApplyRoundedWindowRegion();
        StateChanged += (_, __) => ApplyRoundedWindowRegion();
        SizeChanged += (_, __) => UpdateContentHostSize();
        _rootGrid.PreviewMouseDown += RootGrid_PreviewMouseDown;
    }



    private static void RegisterWindowForTab(string tabId, ChildBrowserWindow window)
    {
        lock (_lock)
        {
            if (!_windowsByTab.TryGetValue(tabId, out var list))
            {
                list = [];
                _windowsByTab[tabId] = list;
            }
            if (!list.Contains(window)) list.Add(window);
        }
    }

    private static void UnregisterWindowForTab(string tabId, ChildBrowserWindow window)
    {
        lock (_lock)
        {
            if (_windowsByTab.TryGetValue(tabId, out var list))
            {
                list.Remove(window);
                if (list.Count == 0) _windowsByTab.Remove(tabId);
            }
        }
    }

    private static void ShowWindowsForTab(string tabId)
    {
        List<ChildBrowserWindow>? list;
        lock (_lock) _windowsByTab.TryGetValue(tabId, out list);
        if (list == null) return;
        foreach (var w in list.ToArray())
        {
            try
            {
                w.Dispatcher.Invoke(() =>
                {
                    w.UpdateOverlayBounds();
                    w.ApplyRoundedWindowRegion();
                    w.UpdateContentHostSize();
                    w.Show();
                });
            }
            catch { }
        }
    }

    private static void HideWindowsForTab(string tabId)
    {
        List<ChildBrowserWindow>? list;
        lock (_lock) _windowsByTab.TryGetValue(tabId, out list);
        if (list == null) return;
        foreach (var w in list.ToArray())
        {
            try { w.Dispatcher.Invoke(w.Hide); } catch { }
        }
    }

    private static void CloseWindowsForTab(string tabId)
    {
        List<ChildBrowserWindow>? list;
        lock (_lock) _windowsByTab.TryGetValue(tabId, out list);
        if (list == null) return;
        foreach (var w in list.ToArray())
        {
            try { w.Dispatcher.Invoke(w.Close); } catch { }
        }
    }

    private void OnLoadedAttachToOwner(object? sender, RoutedEventArgs e)
    {
        _ownerWindow = Owner ?? Window.GetWindow(MainWindow.Instance);
        if (_ownerWindow == null) return;

        _targetElement = MainWindow.Instance.WebContentBorder;
        UpdateOverlayBounds();
        _contentHost.SizeChanged += (_, __) => { UpdateContentCornerClip(); UpdateButtonsPanelPosition(); };
        _buttonsPanel.SizeChanged += (_, __) => UpdateButtonsPanelPosition();
        UpdateContentHostSize();

        _ownerWindow.LocationChanged += OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.SizeChanged += OwnerWindow_LocationOrSizeChanged;
        _ownerWindow.StateChanged += OwnerWindow_StateChanged;
        if (_targetElement != null)
        {
            _targetElement.SizeChanged += TargetElement_SizeOrLayoutChanged;
            _targetElement.LayoutUpdated += TargetElement_SizeOrLayoutChanged;
        }

        // Start overlay fade-in once sized and positioned
        AnimateOverlayIn();
        UpdateContentCornerClip();
    }

    private void Browser_PageLoadEnded(object? sender, EventArgs e)
    {
        // Run on UI thread and only once
        Dispatcher.BeginInvoke(() =>
        {
            if (_contentAnimationStarted) return;
            _contentAnimationStarted = true;
            AnimateContentIn();
            // Unsubscribe after first main frame load
            try { _browser.PageLoadEnded -= Browser_PageLoadEnded; } catch { }
        });
    }

    private void RootGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Ignore clicks originating from floating buttons
        if (e.OriginalSource is DependencyObject d)
        {
            if (IsDescendantOf(d, _buttonsPanel))
                return;
        }
        // Close only when clicking outside the centered content host
        var pos = e.GetPosition(_contentHost);
        if (pos.X < 0 || pos.Y < 0 || pos.X > _contentHost.ActualWidth || pos.Y > _contentHost.ActualHeight)
        {
            e.Handled = true;
            BeginCloseWithFade();
            AnimateContentOut();
        }
    }

    private void OwnerWindow_StateChanged(object? sender, EventArgs e)
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
        OwnerWindow_LocationOrSizeChanged(sender!, e);
    }

    private void OwnerWindow_LocationOrSizeChanged(object? sender, EventArgs e)
    {
        if (_ownerWindow == null) return;
        UpdateOverlayBounds();
    }

    private void TargetElement_SizeOrLayoutChanged(object? sender, EventArgs e) => UpdateOverlayBounds();

    private void UpdateOverlayBounds()
    {
        if (_ownerWindow == null || _targetElement == null) return;
        if (!IsLoaded) return;

        // Get position of WebContentBorder relative to owner window (DIPs)
        var tl = _targetElement.TranslatePoint(new Point(0, 0), _ownerWindow);
        var w = _targetElement.ActualWidth;
        var h = _targetElement.ActualHeight;
        if (w <= 0 || h <= 0) return;

        Left = _ownerWindow.Left + tl.X;
        Top = _ownerWindow.Top + tl.Y;
        Width = w;
        Height = h;
    }

    private void DetachOwnerHandlers()
    {
        if (_targetElement != null)
        {
            _targetElement.SizeChanged -= TargetElement_SizeOrLayoutChanged;
            _targetElement.LayoutUpdated -= TargetElement_SizeOrLayoutChanged;
            _targetElement = null;
        }
        try { _browser.PageLoadEnded -= Browser_PageLoadEnded; } catch { }
        if (_ownerWindow != null)
        {
            _ownerWindow.LocationChanged -= OwnerWindow_LocationOrSizeChanged;
            _ownerWindow.SizeChanged -= OwnerWindow_LocationOrSizeChanged;
            _ownerWindow.StateChanged -= OwnerWindow_StateChanged;
            _ownerWindow = null;
        }
    }

    // Hook into DPI changes and apply rounded corner window region
    private void TryHookDpiAndApplyRoundedCorners()
    {
        var src = (HwndSource?)PresentationSource.FromVisual(this);
        src?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_DPICHANGED = 0x02E0;
        if (msg == WM_DPICHANGED)
        {
            // On DPI change, recompute bounds and visuals to stay aligned with owner content
            Dispatcher.BeginInvoke(() =>
            {
                UpdateOverlayBounds();
                UpdateContentHostSize();
                ApplyRoundedWindowRegion();
            });
        }
        return IntPtr.Zero;
    }

    private void ApplyRoundedWindowRegion()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            var src = (HwndSource?)PresentationSource.FromVisual(this);
            double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

            int w = (int)Math.Max(1, Math.Round(ActualWidth * scaleX));
            int h = (int)Math.Max(1, Math.Round(ActualHeight * scaleY));
            int r = (int)Math.Round(_cornerRadiusDip * (scaleX + scaleY) / 2.0);
            r = Math.Max(1, r);

            nint rgn = WindowInterop.CreateRoundRectRgn(0, 0, w + 1, h + 1, r * 2, r * 2);
            if (rgn != nint.Zero)
            {
                var ok = WindowInterop.SetWindowRgn(hwnd, rgn, true);
                if (ok == 0)
                {
                    WindowInterop.DeleteObject(rgn);
                }
            }
        }
        catch { }
    }

    private void UpdateContentHostSize()
    {
        // Scale down inner browser to, e.g., 85% of container size, keep minimums
        var targetW = Math.Max(400, ActualWidth * 0.85);
        var targetH = Math.Max(300, ActualHeight * 0.85);
        _contentHost.Width = targetW;
        _contentHost.Height = targetH;
        // Ensure overlay container matches content size for proper button placement
        _contentContainer.Width = targetW;
        _contentContainer.Height = targetH;
        UpdateButtonsPanelPosition();
    }

    private void AnimateOverlayIn()
    {
        var fade = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(_overlayFadeDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _overlayBrush.BeginAnimation(Brush.OpacityProperty, fade);
    }

    private void AnimateContentIn()
    {
        // Ensure we have a ScaleTransform as LayoutTransform
        if (_contentHost.LayoutTransform is not ScaleTransform scale)
        {
            scale = new ScaleTransform(0.5, 0.5);
            _contentHost.LayoutTransform = scale;
        }

        // Opacity: 0 -> 1
        var opacityAnim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _contentHost.BeginAnimation(UIElement.OpacityProperty, opacityAnim);

        // Show and fade-in the buttons in sync with content
        _buttonsPanel.Visibility = Visibility.Visible;
        var btnOpacityAnim = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _buttonsPanel.BeginAnimation(UIElement.OpacityProperty, btnOpacityAnim);

        // Scale: 0.5 -> 1.0 (both axes)
        var scaleAnim = new DoubleAnimation
        {
            From = 0.5,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    private void AnimateContentOut()
    {
        // Ensure we have a ScaleTransform as LayoutTransform
        var scale = new ScaleTransform(1.0, 1.0);
        _contentHost.LayoutTransform = scale;
        // Opacity: 1 -> 0
        var opacityAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        _contentHost.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
        // Buttons fade-out and collapse after animation
        var btnOpacityAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        btnOpacityAnim.Completed += (_, __) => _buttonsPanel.Visibility = Visibility.Collapsed;
        _buttonsPanel.BeginAnimation(UIElement.OpacityProperty, btnOpacityAnim);
        // Scale: 1.0 -> 0.5 (both axes)
        var scaleAnim = new DoubleAnimation
        {
            From = 1.0,
            To = 0.5,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
    }

    private void UpdateContentCornerClip()
    {
        var w = _contentHost.ActualWidth;
        var h = _contentHost.ActualHeight;
        if (w <= 0 || h <= 0)
        {
            _contentHost.Clip = null;
            return;
        }
        var radius = 8.0; // match CornerRadius
        _contentHost.Clip = new RectangleGeometry(new Rect(0, 0, w, h), radius, radius);
    }

    private void UpdateButtonsPanelPosition()
    {
        if (_buttonsPanel.RenderTransform is not TranslateTransform tt) return;
        // Move the panel just outside the right edge of the content (top-aligned)
        const double horizontalGap = 24.0;
        var w = _contentHost.ActualWidth;
        if (w <= 0) return;
        tt.X = (w / 2.0) + horizontalGap;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isClosing)
        {
            e.Cancel = true;
            BeginCloseWithFade();
            AnimateContentOut();
            return;
        }
        base.OnClosing(e);
    }

    private void BeginCloseWithFade()
    {
        if (_isClosing) return;
        _isClosing = true;

        var fade = new DoubleAnimation
        {
            To = 0.0,
            Duration = TimeSpan.FromMilliseconds(_overlayFadeDuration),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, __) =>
        {
            if (_browser == null) return;
            // Stop animation and close
            _overlayBrush.BeginAnimation(Brush.OpacityProperty, null);
            try { _browser.Dispose(); } catch { }
            try { Close(); } catch { }
        };
        _overlayBrush.BeginAnimation(Brush.OpacityProperty, fade);
    }

    private static bool IsDescendantOf(DependencyObject child, DependencyObject potentialAncestor)
    {
        var current = child;
        while (current != null)
        {
            if (current == potentialAncestor)
                return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private static Button CreateCircleButton(UIElement icon, string toolTip)
    {
        var btn = new Button
        {
            Width = 36,
            Height = 36,
            ToolTip = toolTip,
            Background = Brushes.Black,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            RenderTransformOrigin = new Point(0.5, 0.5),
            Cursor = Cursors.Hand,
            Content = icon
        };

        // Hover scale animation (10%)
        var rt = new ScaleTransform(1.0, 1.0);
        btn.RenderTransform = rt;
        btn.MouseEnter += (_, __) =>
        {
            var anim = new DoubleAnimation(1.1, TimeSpan.FromMilliseconds(120)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            rt.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            rt.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        };
        btn.MouseLeave += (_, __) =>
        {
            var anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(120)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            rt.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            rt.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        };

        // Circular template
        var borderFactory = new FrameworkElementFactory(typeof(Border));
        borderFactory.SetValue(Border.BackgroundProperty, Brushes.Black);
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(18));
        borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        borderFactory.AppendChild(presenter);

        btn.Template = new ControlTemplate(typeof(Button)) { VisualTree = borderFactory };

        return btn;
    }

    private static Grid CreateIconX() =>
        CreateIcon(
            new()
            {
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Data = Geometry.Parse("M2,2 L14,14")
            },
            new()
            {
                Stroke = Brushes.White,
                StrokeThickness = 2,
                Data = Geometry.Parse("M2,14 L14,2")
            }
        );

    private static Grid CreateIconPopOut() =>
        CreateIcon(
            new()
            {
                Stroke = Brushes.White,
                StrokeThickness = 1.8,
                Data = Geometry.Parse("M2,5 H9 V13 H2 Z")
            },
            new()
            {
                Stroke = Brushes.White,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = Geometry.Parse("M7,9 L14,2 M10,2 L14,2 L14,6")
            }
        );

    private static Grid CreateIcon(params Path[] paths)
    {
        var grid = new Grid { Width = 16, Height = 16 };
        foreach (var p in paths)
            grid.Children.Add(p);
        return grid;
    }
}
