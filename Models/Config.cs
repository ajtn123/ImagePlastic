using Avalonia.Controls;
using Avalonia.Media;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImagePlastic.Models
{
    public class Config
    {
        public FileInfo? DefaultFile { get; set; }
        public IReadOnlyList<WindowTransparencyLevel> Blur { get; set; } = new List<WindowTransparencyLevel>() { WindowTransparencyLevel.AcrylicBlur };
        public IBrush BackgroundColor { get; set; } = Brushes.Transparent;
        public string[] Extensions { get; set; } = Enum.GetValues(typeof(MagickFormat)).Cast<MagickFormat>().Select((a, b) => { return a.ToString().ToLower(); }).ToArray();
        public bool WindowSizeAuto { get; set; } = true;
        public int ExtendImageToTitleBar { get; set; } = 1;
    }
}
