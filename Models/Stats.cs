using ImageMagick;
using ImagePlastic.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ImagePlastic.Models;

//Actually necessary, doesn't it?
public class Stats : ReactiveObject
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
        EditCmd = stats.EditCmd;
        Format = stats.Format;
        Url = stats.Url;

        if (File != null && Success)
            Optimizable = Constants.OptimizableExts.Contains(File.Extension);
    }
    public bool Success { get; }
    [Reactive]
    public bool IsWeb { get; set; } = false;
    [Reactive]
    public string? Url { get; set; }
    [Reactive]
    public FileInfo? File { get; set; }
    [Reactive]
    public string? DisplayName { get; set; }
    [Reactive]
    public int? FileIndex { get; set; }
    [Reactive]
    public int? FileCount { get; set; }
    [Reactive]
    public double Height { get; set; } = double.NaN;
    [Reactive]
    public double Width { get; set; } = double.NaN;
    [Reactive]
    public bool Optimizable { get; set; } = false;
    [Reactive]
    public ProcessStartInfo? EditCmd { get; set; }
    [Reactive]
    public MagickFormat Format { get; set; }
}
