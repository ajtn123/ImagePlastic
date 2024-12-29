using Avalonia;
using System.IO;

namespace ImagePlastic.Models;

//Actually necessary, doesn't it?
public class Stats
{
    public Stats(bool success, Stats? stats = null)
    {
        Success = success;

        if (stats == null) return;
        FileIndex = stats.FileIndex;
        ImageDimension = stats.ImageDimension;
        File = stats.File;
        FileCount = stats.FileCount;
    }
    public bool Success { get; }
    public FileInfo? File { get; set; }
    public int? FileIndex { get; set; }
    public int? FileCount { get; set; }
    public Size? ImageDimension { get; set; }
}
