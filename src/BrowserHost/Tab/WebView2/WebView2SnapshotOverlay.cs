using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BrowserHost.Tab.WebView2;

internal sealed class WebView2SnapshotOverlay
{
    private readonly Grid _root = new();
    private readonly Image _image;
    public bool IsActive { get; private set; }

    public WebView2SnapshotOverlay()
    {
        _image = new Image
        {
            Stretch = Stretch.Fill,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            Visibility = System.Windows.Visibility.Collapsed,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true
        };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.LowQuality);
        _root.Children.Add(_image);
    }

    public System.Windows.UIElement Visual => _root;

    public async Task<bool> TryActivateAsync(CoreWebView2 core)
    {
        if (IsActive) return true;
        await CaptureAsync(core);
        if (_image.Source == null) return false;
        _image.Visibility = System.Windows.Visibility.Visible;
        await AwaitRenderAsync();
        IsActive = true;
        return true;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        _image.Visibility = System.Windows.Visibility.Collapsed;
    }

    private async Task CaptureAsync(CoreWebView2 core)
    {
        try
        {
            using var ms = new MemoryStream();
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
            ms.Position = 0;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            bmp.Freeze();
            _image.Source = bmp;
        }
        catch
        {
        }
    }

    private static Task AwaitRenderAsync()
    {
        var tcs = new TaskCompletionSource();
        void Handler(object? s, EventArgs e)
        {
            CompositionTarget.Rendering -= Handler;
            tcs.TrySetResult();
        }
        CompositionTarget.Rendering += Handler;
        return tcs.Task;
    }
}
