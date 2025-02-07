using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using ImageMagick;
using System.Collections.Generic;
using System.IO;

namespace ImagePlastic.Models;

public class Config
{
    public FileInfo? DefaultFile { get; set; }
    public Transparency[] Blurs { get; set; }
        = [Transparency.AcrylicBlur, Transparency.Mica, Transparency.Blur, Transparency.Transparent];
    public string BackgroundColor { get; set; } = "#00ABCDEF";
    public string[] Extensions { get; set; }
        = [".png", ".jpg", ".jpeg", ".webp", ".avif", ".heic", ".heif", ".bmp", ".jxl", ".gif", ".psd", ".svg"];
    public bool ExtendImageToTitleBar { get; set; } = true;
    public bool SystemAccentColor { get; set; } = false;
    public byte SystemAccentColorOpacity { get; set; } = 127;
    public string CustomAccentColor { get; set; } = "#7FABCDEF";
    public bool LoadingIndicator { get; set; } = true;
    public StretchMode Stretch { get; set; } = StretchMode.Uniform;
    public bool DeleteConfirmation { get; set; } = true;
    public bool RenameConfirmation { get; set; } = false;
    public bool MoveConfirmation { get; set; } = false;
    public bool RecursiveSearch { get; set; } = false;
    public bool ShowHiddenOrSystemFile { get; set; } = false;
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
    public ArrowButton ArrowButton { get; set; } = ArrowButton.Normal;
    public int ArrowSize { get; set; } = 48;
}

public enum ArrowButton
{
    Normal = 0,
    FullWindow = 1,
    Disabled = 2,
}
public enum Transparency
{
    None,
    Transparent,
    Blur,
    AcrylicBlur,
    Mica,
}