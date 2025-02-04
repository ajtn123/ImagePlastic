using ImageMagick;
using ImagePlastic.Models;
using System.IO;

namespace ImagePlastic.ViewModels;

public class StatsViewModel(Stats stats)
{
    public Stats Stats { get; } = stats;
    public bool Success { get; } = stats.Success;
    public bool IsWeb { get; } = stats.IsWeb;
    public string? Url { get; } = stats.Url;
    public FileInfo? File { get; } = stats.File;
    public string? DisplayName { get; } = stats.DisplayName;
    public int? FileIndex { get; } = stats.FileIndex;
    public int? FileCount { get; } = stats.FileCount;
    public double Height { get; } = stats.Height;
    public double Width { get; } = stats.Width;
    public MagickFormat Format { get; } = stats.Format;
}
