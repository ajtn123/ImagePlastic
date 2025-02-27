using Avalonia.ReactiveUI;
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
            this.WhenAnyValue(x => x.ViewModel!.RelativePosition.PointerX, x => x.ViewModel!.RelativePosition.PointerY)
                .Subscribe(pos => { GetColor(pos.Item1, pos.Item2); });
        });
    }
    public void GetColor(double x, double y)
    {
        if (ViewModel == null || x < 0 || x > 1 || y < 0 || y > 1) return;
        var image = ViewModel.Magick;
        var xpx = (int)Math.Round(image.Width * x);
        var ypx = (int)Math.Round(image.Height * y);
        ViewModel.Pixel = image.GetPixels().GetPixel(xpx, ypx);
        ViewModel.HexColorString = ViewModel.Pixel.ToColor()?.ToHexString();
    }
}