using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImagePlastic.Models;

public class Config
{
    public FileInfo? DefaultFile { get; set; }
    public IReadOnlyList<WindowTransparencyLevel> Blur { get; set; }
        = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.None];
    public IBrush BackgroundColor { get; set; }
        = Brushes.Transparent;
    public string[] Extensions { get; set; }
        //= Enum.GetValues<MagickFormat>().Cast<MagickFormat>().Select((a, b) => { return a.ToString().ToLower().Insert(0, "."); }).ToArray();
        = [".png", ".jpg", ".jpeg", ".avif", ".heic", ".heif", ".bmp", ".jxl", ".gif", ".psd"];
    public bool WindowSizeAuto { get; set; } = true;
    public bool ExtendImageToTitleBar { get; set; } = true;
    public bool LoadingIndicator { get; set; } = true;
    public StretchMode Stretch { get; set; }
        = StretchMode.Uniform;
    public bool Preload { get; set; } = true;
    public int PreloadLeft { get; set; } = 0;
    public int PreloadRight { get; set; } = 2;
    public BitmapInterpolationMode InterpolationMode { get; set; }
        = BitmapInterpolationMode.None;
}
