using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace TheRichScript_WinUI.Dialogs;

public sealed partial class CropAvatarDialog : ContentDialog
{
    private readonly string _sourceFilePath;
    private bool _isDragging;
    private Point _startPointerPosition;
    private double _startTranslateX;
    private double _startTranslateY;

    public string? CroppedFilePath { get; private set; }

    public CropAvatarDialog(string sourceFilePath)
    {
        InitializeComponent();
        RequestedTheme = (App.MainWindowInstance?.Content as FrameworkElement)?.RequestedTheme ?? ElementTheme.Default;
        _sourceFilePath = sourceFilePath;

        Loaded += CropAvatarDialog_Loaded;
    }

    private void CropAvatarDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_sourceFilePath) && File.Exists(_sourceFilePath))
            {
                var bitmap = new BitmapImage(new Uri(_sourceFilePath));
                SourceImage.Source = bitmap;
            }
        }
        catch
        {
            // Ignore load error
        }
    }

    private void SourceImage_ImageOpened(object sender, RoutedEventArgs e)
    {
        if (SourceImage.Source is BitmapImage bmp && bmp.PixelWidth > 0 && bmp.PixelHeight > 0)
        {
            // Scale so that the smaller dimension fills the 360px viewport at 1.0x scale
            double scale = 360.0 / Math.Min(bmp.PixelWidth, bmp.PixelHeight);
            SourceImage.Width = Math.Max(360, bmp.PixelWidth * scale);
            SourceImage.Height = Math.Max(360, bmp.PixelHeight * scale);

            ImageTransform.CenterX = SourceImage.Width / 2.0;
            ImageTransform.CenterY = SourceImage.Height / 2.0;
            ImageTransform.ScaleX = 1.0;
            ImageTransform.ScaleY = 1.0;
            ImageTransform.TranslateX = 0;
            ImageTransform.TranslateY = 0;
            ZoomSlider.Value = 1.0;
        }
    }

    private void Viewport_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _startPointerPosition = e.GetCurrentPoint(CropViewportContainer).Position;
        _startTranslateX = ImageTransform.TranslateX;
        _startTranslateY = ImageTransform.TranslateY;
        CropViewportContainer.CapturePointer(e.Pointer);
    }

    private void Viewport_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;

        var currentPoint = e.GetCurrentPoint(CropViewportContainer).Position;
        var deltaX = currentPoint.X - _startPointerPosition.X;
        var deltaY = currentPoint.Y - _startPointerPosition.Y;

        ImageTransform.TranslateX = _startTranslateX + deltaX;
        ImageTransform.TranslateY = _startTranslateY + deltaY;
    }

    private void Viewport_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
        CropViewportContainer.ReleasePointerCapture(e.Pointer);
    }

    private void Viewport_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
    }

    private void Viewport_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(CropViewportContainer).Properties.MouseWheelDelta;
        if (delta > 0)
        {
            ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 0.1);
        }
        else if (delta < 0)
        {
            ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 0.1);
        }
    }

    private void ZoomSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (ImageTransform != null)
        {
            ImageTransform.ScaleX = e.NewValue;
            ImageTransform.ScaleY = e.NewValue;
        }
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        ZoomSlider.Value = Math.Max(ZoomSlider.Minimum, ZoomSlider.Value - 0.15);
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        ZoomSlider.Value = Math.Min(ZoomSlider.Maximum, ZoomSlider.Value + 0.15);
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (ImageTransform != null)
        {
            ImageTransform.TranslateX = 0;
            ImageTransform.TranslateY = 0;
            ZoomSlider.Value = 1.0;
            ImageTransform.ScaleX = 1.0;
            ImageTransform.ScaleY = 1.0;
        }
    }

    private async void CropAvatarDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            // Temporarily hide overlay stencil so only the photo viewport is rendered
            OverlayStencil.Visibility = Visibility.Collapsed;

            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(CropViewportContainer, 360, 360);

            OverlayStencil.Visibility = Visibility.Visible;

            var pixelBuffer = await rtb.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();

            var tempFolder = ApplicationData.Current.TemporaryFolder;
            var tempFile = await tempFolder.CreateFileAsync($"avatar_crop_{Guid.NewGuid():N}.png", CreationCollisionOption.ReplaceExisting);

            using (var stream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    (uint)rtb.PixelWidth,
                    (uint)rtb.PixelHeight,
                    96,
                    96,
                    pixels);

                await encoder.FlushAsync();
            }

            CroppedFilePath = tempFile.Path;
        }
        catch (Exception)
        {
            OverlayStencil.Visibility = Visibility.Visible;
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }
}
