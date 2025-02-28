using Avalonia.Media.Imaging;
using ImageMagick;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ImagePlastic.ViewModels;

public class ColorPickerWindowViewModel : ViewModelBase
{
    [Reactive]
    public IPixel<float>? Pixel { get; set; }
    [Reactive]
    public string? HexColorString { get; set; }
    [Reactive]
    public int PixelX { get; set; }
    [Reactive]
    public int PixelY { get; set; }
    public RelativePosition RelativePosition { get; set; } = new();
    public MagickImage? Magick => RelativePosition.Magick;
}
public class RelativePosition : ReactiveObject
{
    [Reactive]
    public bool Frozen { get; set; }
    [Reactive]
    public double PointerX { get; set; }
    [Reactive]
    public double PointerY { get; set; }
    [Reactive]
    public MagickImage? Magick { get; set; }
    [Reactive]
    public Bitmap? Bitmap { get; set; }
}

