using Avalonia.Media.Imaging;
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
public class Stats : ReactiveObject, IDisposable
{
    public Stats()
    {
        this.WhenAnyValue(s => s.File).Subscribe(f => Optimizable = Constants.OptimizableExts.Contains(f?.Extension));
    }

    [Reactive]
    public bool Success { get; set; } = true;
    [Reactive]
    public bool IsWeb { get; set; } = false;
    [Reactive]
    public string? Url { get; set; }
    [Reactive]
    public FileInfo? File { get; set; }
    [Reactive]
    public Stream? Stream { get; set; }
    [Reactive]
    public string? DisplayName { get; set; }
    [Reactive]
    public int FileIndex { get; set; } = -1;
    [Reactive]
    public int FileCount { get; set; } = 0;
    [Reactive]
    public bool Optimizable { get; set; } = false;
    [Reactive]
    public ProcessStartInfo? EditCmd { get; set; }
    [Reactive]
    public MagickImageInfo? Info { get; set; }
    [Reactive]
    public MagickImage? Image { get; set; }
    [Reactive]
    public Bitmap? Bitmap { get; set; }
    [Reactive]
    public string? SvgPath { get; set; }

    public MagickFormat Format => Info != null ? Info.Format : default;
    public double Height => Info != null ? Info.Height : double.NaN;
    public double Width => Info != null ? Info.Width : double.NaN;

    public void Dispose()
    {
        Image?.Dispose();
        Bitmap?.Dispose();
        Stream?.Dispose();
        GC.SuppressFinalize(this);
    }
}
