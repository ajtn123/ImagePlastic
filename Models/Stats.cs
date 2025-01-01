using Avalonia;
using System.IO;

namespace ImagePlastic.Models;

//Actually necessary, doesn't it?
public class Stats
{
    private string? displayName;
    public Stats(bool success, Stats? stats = null)
    {
        Success = success;

        if (stats == null) return;
        IsWeb = stats.IsWeb;
        FileIndex = stats.FileIndex;
        ImageDimension = stats.ImageDimension;
        File = stats.File;
        FileCount = stats.FileCount;
        DisplayName = stats.DisplayName;
    }
    public bool Success { get; }
    public bool IsWeb { get; set; } = false;
    public FileInfo? File { get; set; }
    public string? DisplayName { get => displayName ?? File.Name; set => displayName = value; }
    public int? FileIndex { get; set; }
    public int? FileCount { get; set; }
    public Size? ImageDimension { get; set; }
}
