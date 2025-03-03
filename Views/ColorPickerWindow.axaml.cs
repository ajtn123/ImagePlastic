using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;

namespace ImagePlastic.Views;

public partial class ColorPickerWindow : ReactiveWindow<ColorPickerWindowViewModel>
{
    public ColorPickerWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            if (ViewModel == null) return;

            WindowStartupLocation = WindowStartupLocation.Manual;
            DraggableBehavior.SetIsDraggable(this);
            if ((MainWindow?)Owner != null)
                Position = new(((MainWindow)Owner).Position.X + 25, ((MainWindow)Owner).Position.Y + 25);

            this.WhenAnyValue(x => x.ViewModel!.PixelX, x => x.ViewModel!.PixelY)
                .Subscribe(pos => { GetColor(pos.Item1, pos.Item2); });
            this.WhenAnyValue(a => a.ViewModel!.RelativePosition.PointerX)
                .Subscribe(x =>
                {
                    if (ViewModel.Magick != null)
                        ViewModel.PixelX = (int)Math.Clamp((int)Math.Round(ViewModel.Magick.Width * x), 0, ViewModel.Magick.Width - 1);
                });
            this.WhenAnyValue(a => a.ViewModel!.RelativePosition.PointerY)
                .Subscribe(y =>
                {
                    if (ViewModel.Magick != null)
                        ViewModel.PixelY = (int)Math.Clamp((int)Math.Round(ViewModel.Magick.Height * y), 0, ViewModel.Magick.Height - 1);
                });
        });
    }

    public double Scaling => Screens.ScreenFromWindow(this)!.Scaling;
    public void GetColor(int x, int y)
    {
        if (ViewModel?.Magick == null) return;
        var image = ViewModel.Magick;
        if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;

        Canvas.SetLeft(MagnifiedImage, -10 * x + 95);
        Canvas.SetTop(MagnifiedImage, -10 * y + 95);

        using ImageMagick.IPixelCollection<float> pixels = image.GetPixels();
        ViewModel.Pixel = pixels.GetPixel(x, y);
        ViewModel.HexColorString = ViewModel.Pixel.ToColor()?.ToHexString();
    }
    public void CopyColor()
    {
        if (ViewModel == null || !ViewModel.Config.ColorPickerCopy || Clipboard == null) return;
        Clipboard.SetTextAsync(ViewModel.HexColorString);
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel!.RelativePosition.Frozen = !ViewModel.RelativePosition.Frozen;
    private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => CopyColor();
    private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Close();
}