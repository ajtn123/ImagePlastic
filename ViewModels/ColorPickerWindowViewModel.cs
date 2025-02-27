using ImageMagick;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ImagePlastic.ViewModels;

public class ColorPickerWindowViewModel(MagickImage magick) : ViewModelBase
{
    [Reactive]
    public IPixel<float>? Pixel { get; set; }
    [Reactive]
    public string? HexColorString { get; set; }
    public RelativePosition RelativePosition { get; set; } = new();
    public MagickImage Magick { get; set; } = magick;
}
public class RelativePosition : ReactiveObject
{
    [Reactive]
    public double PointerX { get; set; }
    [Reactive]
    public double PointerY { get; set; }
}

