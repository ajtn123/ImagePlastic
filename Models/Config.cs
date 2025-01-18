using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ImageMagick;
using System.Collections.Generic;
using System.IO;

namespace ImagePlastic.Models;

public class Config
{
    public FileInfo? DefaultFile { get; set; }
    public IReadOnlyList<WindowTransparencyLevel> Blur { get; set; }
        = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent];
    public IBrush BackgroundColor { get; set; } = Brush.Parse("#00BFBFBF");
    public string[] Extensions { get; set; }
        = [".png", ".jpg", ".jpeg", ".avif", ".heic", ".heif", ".bmp", ".jxl", ".gif", ".psd", ".svg"];
    //This includes too many non-image exts.
    //= Enum.GetValues<MagickFormat>().Cast<MagickFormat>().Select((a, b) => { return a.ToString().ToLower().Insert(0, "."); }).ToArray();
    public bool ExtendImageToTitleBar { get; set; } = true;
    public bool SystemAccentColor { get; set; } = true;
    public Color CustomAccentColor { get; set; } = Color.Parse("#7F40CFBF");
    public bool LoadingIndicator { get; set; } = true;
    public StretchMode Stretch { get; set; } = StretchMode.Uniform;
    public bool DeleteConfirmation { get; set; } = false;
    public bool Preload { get; set; } = true;
    public int PreloadLeft { get; set; } = 0;
    public int PreloadRight { get; set; } = 2;
    public BitmapInterpolationMode InterpolationMode { get; set; }
        = BitmapInterpolationMode.HighQuality;
    public ButtonName PanButton { get; set; } = ButtonName.Left;
    //Value of null = default;
    //Value of empty = disabled.
    public Dictionary<MagickFormat, string?> EditApp = new(){
        {default,@"C:\Program Files\GIMP 2\bin\gimp-2.10.exe"},
    };
}
