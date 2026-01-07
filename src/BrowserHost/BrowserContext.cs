using BrowserHost.Features;
using BrowserHost.Features.ActionContext.Tabs;
using BrowserHost.Features.CustomWindowChrome;
using BrowserHost.Features.DragDrop;
using BrowserHost.Interop;
using BrowserHost.Tab;
using CefSharp;
using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace BrowserHost;

public class BrowserContext : IBrowserContext
{
    private readonly MainWindow _window;

    private bool _actionContextHidden;
    private bool _actionContextInitialized;
    private DispatcherTimer? _actionContextMinWidthTimer;
    private double? _actionContextMinWidth;

    private bool _customWindowChromeIntegrated;
    private CustomWindowChromeBrowserApi? _customWindowChromeApi;
    private Rect? _customWindowChromeLastNormalBounds;
    private bool _customWindowChromeApplyingRestoreBounds;

    public event Action? ActionContextResizeCompleted;
    public event Action? TabPaletteResizeCompleted;

    public BrowserContext(MainWindow window)
    {
        _window = window;

        DragDropHost = new MainWindowDragDropHost(_window);
        TabsHost = new MainWindowTabsHost(_window);

        _window.ActionContextGridSplitter.DragCompleted += (_, __) => ActionContextResizeCompleted?.Invoke();
        _window.TabPaletteGridSplitter.DragCompleted += (_, __) => TabPaletteResizeCompleted?.Invoke();
    }

    public Options AppOptions => App.Options;

    public ITabBrowser? CurrentTab => _window.CurrentTab;
    public IDragDropHost DragDropHost { get; }
    public ITabsHost TabsHost { get; }
    public string? CurrentTabId => _window.CurrentTab?.Id;
    public ModifierKeys CurrentKeyboardModifiers => Keyboard.Modifiers;

    public Color WorkspaceColor
    {
        get => _window.WorkspaceColor;
        set => _window.WorkspaceColor = value;
    }

    public TFeature GetFeature<TFeature>() where TFeature : Feature =>
        _window.GetFeature<TFeature>();

    public WindowState WindowState
    {
        get => _window.WindowState;
        set => _window.WindowState = value;
    }

    public void SetClipboardText(string text) => Clipboard.SetText(text);

    public void SetCurrentTab(ITabBrowser? tab)
    {
        if (tab is null)
        {
            _window.SetCurrentTab(null);
            return;
        }

        if (tab is not TabBrowser tabBrowser)
            throw new InvalidOperationException("Cannot set a non-TabBrowser instance as current tab.");

        _window.SetCurrentTab(tabBrowser);
    }

    public ITabBrowser CreateNewTab(string address, TabsBrowserApi tabsApi, global::BrowserHost.Utilities.PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
        CreateExistingTab($"{Guid.NewGuid()}", address, tabsApi, pubSub, setManualAddress, favicon, isChildBrowser);

    public ITabBrowser CreateExistingTab(string tabId, string address, TabsBrowserApi tabsApi, global::BrowserHost.Utilities.PubSub pubSub, bool setManualAddress, string? favicon, bool isChildBrowser) =>
        new TabBrowser(tabId, address, tabsApi, pubSub, setManualAddress: setManualAddress, favicon: favicon, isChildBrowser: isChildBrowser);

    public void ToggleActionContextDevTools() => ToggleDevTools(_window.ActionContext.GetBrowserHost());
    public void ToggleTabPaletteDevTools() => ToggleDevTools(_window.TabPaletteBrowserControl.GetBrowserHost());

    public bool IsActionDialogVisible => _window.ActionDialog.Visibility == Visibility.Visible;

    public void ShowActionDialogControl()
    {
        if (_window.ActionDialog.Visibility == Visibility.Visible)
            return;

        _window.ActionDialog.Opacity = 0;
        _window.ActionDialog.Visibility = Visibility.Visible;
        _window.ActionDialog.Focus();

        if (_window.ActionDialog.RenderTransform is not ScaleTransform)
        {
            var scale = new ScaleTransform(0, 0, 0.5, 0.5);
            _window.ActionDialog.RenderTransform = scale;
            _window.ActionDialog.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        else
        {
            ((ScaleTransform)_window.ActionDialog.RenderTransform).ScaleX = 0;
            ((ScaleTransform)_window.ActionDialog.RenderTransform).ScaleY = 0;
        }

        var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(250)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        _window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        ((ScaleTransform)_window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleXProperty, scaleIn);
        ((ScaleTransform)_window.ActionDialog.RenderTransform).BeginAnimation(ScaleTransform.ScaleYProperty, scaleIn);
    }

    public void HideActionDialogControl()
    {
        if (_window.ActionDialog.Visibility == Visibility.Hidden)
            return;

        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250)));
        var scaleOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(250)))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, __) =>
        {
            _window.ActionDialog.Visibility = Visibility.Hidden;
        };
        _window.ActionDialog.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        if (_window.ActionDialog.RenderTransform is ScaleTransform scale)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleOut);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleOut);
        }
    }

    public void ToggleActionContextVisibility()
    {
        var column = _window.ActionContextColumn;
        var splitterCol = _window.ActionContextSplitterColumn;
        var splitter = _window.ActionContextGridSplitter;
        var browser = _window.ActionContext;

        if (!_actionContextInitialized)
        {
            _window.InitializeSidePanel(column, splitterCol, 8, isExpanded: true);
            _actionContextMinWidth = column.MinWidth;
            _actionContextInitialized = true;
        }

        StopActionContextTimerFromPreviousAnimation();

        if (!_actionContextHidden)
        {
            var duration = TimeSpan.FromMilliseconds(200);
            column.MinWidth = 0;
            _window.CollapseSidePanel(column, splitterCol, browser, splitter, duration);
            _actionContextHidden = true;
        }
        else
        {
            var duration = TimeSpan.FromMilliseconds(300);

            if (splitterCol.Width.Value == 0)
                splitterCol.Width = new GridLength(8);

            _window.ExpandSidePanel(column, splitterCol, browser, splitter, duration);

            RestoreActionContextMinimumWidthAfterAnimation(column, duration);
            _actionContextHidden = false;
        }
    }

    private void RestoreActionContextMinimumWidthAfterAnimation(ColumnDefinition column, TimeSpan duration)
    {
        _actionContextMinWidthTimer = new() { Interval = duration };
        _actionContextMinWidthTimer.Tick += (_, __) =>
        {
            _actionContextMinWidthTimer?.Stop();
            column.MinWidth = _actionContextMinWidth ?? 200;
        };
        _actionContextMinWidthTimer.Start();
    }

    private void StopActionContextTimerFromPreviousAnimation()
    {
        _actionContextMinWidthTimer?.Stop();
        _actionContextMinWidthTimer = null;
    }

    public double ActionContextActualWidth => _window.ActionContextColumn.ActualWidth;

    public void SetActionContextWidth(double width)
    {
        if (width > 0)
            _window.ActionContextColumn.Width = new GridLength(width);
    }

    public double TabPaletteActualWidth => _window.TabPaletteColumn.ActualWidth;

    public void ShowTabPalette() => _window.ShowTabPalette();
    public void HideTabPalette() => _window.HideTabPalette();
    public void FocusTabPalette() => _window.TabPaletteBrowserControl.Focus();

    public void EnableCustomWindowChromeIntegration(CustomWindowChromeBrowserApi customWindowChromeApi)
    {
        if (_customWindowChromeIntegrated)
            return;

        _customWindowChromeIntegrated = true;
        _customWindowChromeApi = customWindowChromeApi;

        _window.WindowStyle = WindowStyle.None;
        _window.AllowsTransparency = true;

        _window.ChromeUI.PreviewMouseLeftButtonDown += ChromeUI_PreviewMouseLeftButtonDown;

        _window.ResizeBorder.PreviewMouseMove += ResizeBorder_PreviewMouseMove;
        _window.ResizeBorder.PreviewMouseLeftButtonDown += ResizeBorder_PreviewMouseLeftButtonDown;
        _window.StateChanged += Window_StateChanged;
        _window.LocationChanged += (_, __) => CaptureCustomWindowChromeNormalBounds();
        _window.SizeChanged += (_, __) => CaptureCustomWindowChromeNormalBounds();

        CaptureCustomWindowChromeNormalBounds();
    }

    public bool ActionRequiresDispatch
    {
        get
        {
            var dispatcher = Application.Current?.Dispatcher;
            return dispatcher is not null && !dispatcher.CheckAccess();
        }
    }

    public void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("Dispatcher is not available. Use ActionRequiresDispatch to verify if dispatching is needed.");
        dispatcher.Invoke(action);
    }

    private static void ToggleDevTools(IBrowserHost? browserHost)
    {
        if (browserHost != null)
        {
            if (browserHost.HasDevTools)
                browserHost.CloseDevTools();
            else
                browserHost.ShowDevTools();
        }
    }

    private void CaptureCustomWindowChromeNormalBounds()
    {
        if (_customWindowChromeApplyingRestoreBounds) return;
        if (_window.WindowState == WindowState.Normal && _window.ActualWidth > 0 && _window.ActualHeight > 0)
        {
            _customWindowChromeLastNormalBounds = new Rect(_window.Left, _window.Top, _window.Width, _window.Height);
        }
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        var isMaximized = _window.WindowState == WindowState.Maximized;
        _customWindowChromeApi?.UpdateWindowState(isMaximized);

        if (isMaximized)
            CaptureCustomWindowChromeNormalBounds();
    }

    private void ChromeUI_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && IsMouseOverTransparentPixel(e))
        {
            ToggleMaximizedState();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed && IsMouseOverTransparentPixel(e))
        {
            if (_window.WindowState == WindowState.Maximized)
            {
                BeginDetachDragFromMaximized(e);
                return;
            }

            try { _window.DragMove(); } catch { }
        }
    }

    private static bool IsMouseOverTransparentPixel(MouseEventArgs e)
    {
        if (e.OriginalSource is Image source && source.Source is BitmapSource bitmap)
        {
            var pos = e.GetPosition(source);
            int x = (int)(pos.X * bitmap.PixelWidth / source.ActualWidth);
            int y = (int)(pos.Y * bitmap.PixelHeight / source.ActualHeight);
            if (x >= 0 && y >= 0 && x < bitmap.PixelWidth && y < bitmap.PixelHeight)
            {
                byte[] pixels = new byte[4];
                bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixels, 4, 0);
                return pixels[3] == 0; // alpha
            }
        }
        return false;
    }

    private void ToggleMaximizedState()
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else
            WindowState = WindowState.Maximized;
    }

    private void BeginDetachDragFromMaximized(MouseEventArgs e)
    {
        var wa = GetCurrentMonitorWorkAreaDip();
        var posInWindow = e.GetPosition(_window);
        double percentX = posInWindow.X / _window.ActualWidth;
        double percentY = posInWindow.Y / _window.ActualHeight;

        var restore = _customWindowChromeLastNormalBounds ?? _window.RestoreBounds;
        if (restore.Width < 50 || restore.Height < 50)
        {
            restore = new Rect(wa.Left + wa.Width * 0.1, wa.Top + wa.Height * 0.1, wa.Width * 0.8, wa.Height * 0.8);
        }

        if (MonitorInterop.GetCursorPos(out var pt))
        {
            var src = (HwndSource?)PresentationSource.FromVisual(_window);
            double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
            double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;
            double cursorX = pt.X / scaleX;
            double cursorY = pt.Y / scaleY;

            WindowState = WindowState.Normal; // triggers restore

            double targetWidth = restore.Width;
            double targetHeight = restore.Height;

            double newLeft = cursorX - targetWidth * percentX;
            double newTop = cursorY - targetHeight * percentY;

            newLeft = Math.Clamp(newLeft, wa.Left, wa.Right - targetWidth);
            newTop = Math.Clamp(newTop, wa.Top, wa.Bottom - targetHeight);

            _customWindowChromeApplyingRestoreBounds = true;
            try
            {
                _window.Left = newLeft;
                _window.Top = newTop;
                _window.Width = targetWidth;
                _window.Height = targetHeight;
            }
            finally { _customWindowChromeApplyingRestoreBounds = false; }

            CaptureCustomWindowChromeNormalBounds();
            _window.UpdateLayout();
            try { _window.DragMove(); } catch { }
        }
    }

    private Rect GetCurrentMonitorWorkAreaDip()
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == nint.Zero)
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        var hMon = MonitorInterop.MonitorFromWindow(hwnd, MonitorInterop.MONITOR_DEFAULTTONEAREST);
        var mi = new MonitorInterop.MONITORINFO() { cbSize = Marshal.SizeOf<MonitorInterop.MONITORINFO>() };
        if (!MonitorInterop.GetMonitorInfo(hMon, ref mi))
        {
            var wa = SystemParameters.WorkArea;
            return new Rect(wa.Left, wa.Top, wa.Width, wa.Height);
        }

        var src = (HwndSource?)PresentationSource.FromVisual(_window);
        double scaleX = src?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
        double scaleY = src?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

        double left = mi.rcWork.Left / scaleX;
        double top = mi.rcWork.Top / scaleY;
        double width = (mi.rcWork.Right - mi.rcWork.Left) / scaleX;
        double height = (mi.rcWork.Bottom - mi.rcWork.Top) / scaleY;
        return new Rect(left, top, width, height);
    }

    private void ResizeBorder_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            var pos = e.GetPosition(_window.ResizeBorder);
            var hit = GetResizeDirection(pos, _window.ResizeBorder.ActualWidth, _window.ResizeBorder.ActualHeight);
            _window.Cursor = GetCursorForResizeDirection(hit);
        }
        else
        {
            _window.Cursor = Cursors.Arrow;
        }
    }

    private void ResizeBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (WindowState == WindowState.Normal && e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(_window.ResizeBorder);
            var hit = GetResizeDirection(pos, _window.ResizeBorder.ActualWidth, _window.ResizeBorder.ActualHeight);
            if (hit != HitTest.HTNOWHERE)
                ResizeWindow(hit);
        }
    }

    private enum HitTest
    {
        HTNOWHERE = 0,
        HTLEFT = 10,
        HTRIGHT = 11,
        HTTOP = 12,
        HTTOPLEFT = 13,
        HTTOPRIGHT = 14,
        HTBOTTOM = 15,
        HTBOTTOMLEFT = 16,
        HTBOTTOMRIGHT = 17
    }

    private static HitTest GetResizeDirection(Point pos, double width, double height)
    {
        const int edge = 8;
        bool left = pos.X >= 0 && pos.X < edge;
        bool right = pos.X <= width && pos.X > width - edge;
        bool top = pos.Y >= 0 && pos.Y < edge;
        bool bottom = pos.Y <= height && pos.Y > height - edge;

        if (left && top) return HitTest.HTTOPLEFT;
        if (right && top) return HitTest.HTTOPRIGHT;
        if (left && bottom) return HitTest.HTBOTTOMLEFT;
        if (right && bottom) return HitTest.HTBOTTOMRIGHT;
        if (left) return HitTest.HTLEFT;
        if (right) return HitTest.HTRIGHT;
        if (top) return HitTest.HTTOP;
        if (bottom) return HitTest.HTBOTTOM;
        return HitTest.HTNOWHERE;
    }

    private static Cursor GetCursorForResizeDirection(HitTest hit) => hit switch
    {
        HitTest.HTLEFT => Cursors.SizeWE,
        HitTest.HTRIGHT => Cursors.SizeWE,
        HitTest.HTTOP => Cursors.SizeNS,
        HitTest.HTBOTTOM => Cursors.SizeNS,
        HitTest.HTTOPLEFT => Cursors.SizeNWSE,
        HitTest.HTTOPRIGHT => Cursors.SizeNESW,
        HitTest.HTBOTTOMLEFT => Cursors.SizeNESW,
        HitTest.HTBOTTOMRIGHT => Cursors.SizeNWSE,
        _ => Cursors.Arrow
    };

    private const int WM_NCLBUTTONDOWN = 0x00A1;

    private void ResizeWindow(HitTest hit)
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        WindowInterop.SendMessage(hwnd, WM_NCLBUTTONDOWN, (nint)hit, nint.Zero);
    }
}
