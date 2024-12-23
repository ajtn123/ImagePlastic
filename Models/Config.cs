using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.Generic;
using System.IO;

namespace ImagePlastic.Models
{
    public class Config
    {
        public FileInfo? DefaultFile { get; set; }
        public IReadOnlyList<WindowTransparencyLevel> Blur { get; set; } = new List<WindowTransparencyLevel>() { WindowTransparencyLevel.AcrylicBlur };
        public IBrush BackgroundColor { get; set; } = Brushes.Transparent;
        public List<string> Extensions { get; set; } = [".jpg", ".png"];
        public bool WindowSizeAuto { get; set; } = true;
        public int ExtendImageToTitleBar { get; set; } = 1;
    }
}
