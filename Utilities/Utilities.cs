﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using ImageMagick;
using ImagePlastic.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImagePlastic.Utilities;

public static class Utils
{
    public static Bitmap? ConvertImage(Stream stream)
    {
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            using var mi = new MagickImage(stream);
            return mi.ConvertToAvaloniaBitmap();
        }
        catch (Exception e) { Trace.WriteLine(e); return null; }
    }
    public static Bitmap? ConvertImage(Stream stream, out MagickImage? image)
    {
        try
        {
            stream.Seek(0, SeekOrigin.Begin);
            image = new MagickImage(stream);
            return image.ConvertToAvaloniaBitmap();
        }
        catch (Exception e) { Trace.WriteLine(e); image = null; return null; }
    }
    public static Bitmap? ConvertImage(FileInfo file)
    {
        using var fs = file.OpenRead();
        return ConvertImage(fs);
    }
    public static Bitmap? ConvertImage(MagickImage image)
    {
        try { return image.ConvertToAvaloniaBitmap(); }
        catch (Exception e) { Trace.WriteLine(e); return null; }
    }

    public static IBrush? GetSystemBrush(double opacity = 0.5)
    {
        if (Application.Current!.TryGetResource("SystemAccentColor", Application.Current.ActualThemeVariant, out object? accentObject))
            return new ImmutableSolidColorBrush((Color)accentObject!, opacity);
        else return null;
    }

    private static readonly HttpClient client = new();
    public static async Task<Stream?> GetStreamFromWeb(string url)
    {
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception e) { Trace.WriteLine(e); return null; }
        return null;
    }

    private static readonly ImageOptimizer optimizer = new() { IgnoreUnsupportedFormats = true };
    public static bool Optimize(FileInfo file)
        => optimizer.LosslessCompress(file);

    public static EqualityComparer<FileInfo?> FileInfoComparer { get; }
        = EqualityComparer<FileInfo?>.Create((a, b) =>
        {
            if (a == null || b == null) return false;
            else return a.FullName.Equals(b.FullName, StringComparison.OrdinalIgnoreCase);
        });
    public static int SeekIndex(int current, int offset, int total)
        => (current + offset + total) % total;

    public static void SelectInExplorer(FileInfo file)
        => Process.Start("explorer.exe", $@"/select,""{file.FullName}""");
    public static void OpenInExplorer(string path)
        => Process.Start("explorer.exe", $@"""{path}""");
    public static ProcessStartInfo? GetEditAppStartInfo(FileInfo file, MagickFormat format, Config? config = null)
    {
        config ??= new();
        if (config.EditApp.TryGetValue(format, out string? app))
            return app != "" ? new ProcessStartInfo
            { FileName = app ?? config.EditApp[default], Arguments = $"\"{file.FullName}\"" } : null;
        else
            return new ProcessStartInfo
            { FileName = config.EditApp[default], Arguments = $"\"{file.FullName}\"" };
    }

    public static string ToReadable(long length)
    {
        var l = Math.Log2(length);
        if (l >= 0 && l < 10)
            return $"{length} Bytes";
        if (l >= 10 && l < 20)
            return $"{double.Round(length / (double)(1 << 10), 2)} KiB";
        if (l >= 20 && l < 30)
            return $"{double.Round(length / (double)(1 << 20), 2)} MiB";
        if (l >= 30)
            return $"{double.Round(length / (double)(1 << 30), 2)} GiB";
        return length.ToString();
    }
}