using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using ImageMagick;
using System.Collections.Generic;
using System.IO;

namespace ImagePlastic.Models;

public class Config
{
    public FileInfo? DefaultFile { get; set; }
    public string BackgroundColor { get; set; } = "#00ABCDEF";
    //Transparent window background color effect.
    public Transparency[] Blurs { get; set; }
        = [Transparency.AcrylicBlur, Transparency.Mica, Transparency.Blur, Transparency.Transparent];
    public string[] Extensions { get; set; }
        = [".png", ".jpg", ".jpeg", ".webp", ".avif", ".heic", ".heif", ".bmp", ".jxl", ".gif", ".psd", ".svg"];
    public bool ExtendImageToTitleBar { get; set; } = true;
    public bool SystemAccentColor { get; set; } = false;
    public byte SystemAccentColorOpacity { get; set; } = 127;
    public string CustomAccentColor { get; set; } = "#7FABCDEF";
    public bool LoadingIndicator { get; set; } = true;
    //Not working.
    public StretchMode Stretch { get; set; } = StretchMode.Uniform;
    public bool MoveToRecycleBin { get; set; } = true;
    public bool DeleteConfirmation { get; set; } = true;
    public bool RenameConfirmation { get; set; } = false;
    public bool MoveConfirmation { get; set; } = false;
    //Show images in the current directory and all its subdirectories, including their own subdirectories, recursively;
    //Enable Recursive in the context menu for single use.
    public bool RecursiveSearch { get; set; } = false;
    public bool ShowHiddenOrSystemFile { get; set; } = false;
    //Preload bitmaps.
    public bool Preload { get; set; } = true;
    public int PreloadLeft { get; set; } = 0;
    public int PreloadRight { get; set; } = 2;
    //Anti-aliasing mode.
    public BitmapInterpolationMode InterpolationMode { get; set; }
        = BitmapInterpolationMode.HighQuality;
    public ButtonName PanButton { get; set; } = ButtonName.Left;
    //Value of null = default;
    //Value of empty = disabled.
    public Dictionary<MagickFormat, string?> EditApp = new(){
        {default,@"C:\Program Files\GIMP 3\bin\gimp-3.exe"},
    };
    //Left and right arrows.
    public ArrowButton ArrowButton { get; set; } = ArrowButton.Normal;
    //For ArrowButton.Normal.
    public int ArrowSize { get; set; } = 48;
    public bool ColorPickerCopy { get; set; } = true;
    //The square at the center of the ColorPicker magnifier, indicating the pixel where the color is picked.
    public bool ColorPickerAiming { get; set; } = true;
    public PathQuotation PathCopyQuotation { get; set; } = PathQuotation.ContainSpace;
    //Close the last child window, if exist, before closing the parent.
    public bool OrderedClosing { get; set; } = true;
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
public enum PathQuotation
{
    ContainSpace,
    No,
    Always,
}