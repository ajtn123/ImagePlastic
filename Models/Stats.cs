using ImagePlastic.Utilities;
using System.IO;
using System.Linq;

namespace ImagePlastic.Models;

//Actually necessary, doesn't it?
public class Stats
{
    public Stats(bool success, Stats? stats = null)
    {
        Success = success;

        if (stats == null) return;
        IsWeb = stats.IsWeb;
        FileIndex = stats.FileIndex;
        Height = stats.Height;
        Width = stats.Width;
        File = stats.File;
        FileCount = stats.FileCount;
        DisplayName = stats.DisplayName;

        if (File != null && Success)
            Optimizable = Constants.OptimizableExts.Contains(File.Extension);
    }
    public bool Success { get; }
    public bool IsWeb { get; set; } = false;
    public FileInfo? File { get; set; }
    public string? DisplayName { get; set; }
    public int? FileIndex { get; set; }
    public int? FileCount { get; set; }
    public double Height { get; set; } = double.NaN;
    public double Width { get; set; } = double.NaN;
    public bool Optimizable { get; set; } = false;
}
