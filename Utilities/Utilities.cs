﻿using Avalonia.Media.Imaging;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImagePlastic.Utilities;

public static class Utils
{
    private static readonly HttpClient client = new();
    private static readonly ImageOptimizer optimizer = new() { IgnoreUnsupportedFormats = true };
    public static EqualityComparer<FileInfo> FileInfoComparer { get; } = EqualityComparer<FileInfo>.Create((a, b) =>
    {
        if (a == null || b == null) return false;
        else return a.FullName.Equals(b.FullName, StringComparison.OrdinalIgnoreCase);
    });
    public static int SeekIndex(int current, int offset, int total)
        => current + offset >= total ? current + offset - total
              : current + offset < 0 ? current + offset + total
                                     : current + offset;

    //Convert any image to a Bitmap.
    public static Bitmap? ConvertImage(Stream stream)
    {
        try
        {
            using var mi = new MagickImage(stream);
            return mi.ToBitmap().ConvertToAvaloniaBitmap();
        }
        catch { return null; }
    }
    public static Bitmap? ConvertImage(FileInfo file)
    {
        using var a = file.OpenRead();
        return ConvertImage(a);
    }
    public static Bitmap? ConvertImage(MagickImage image)
    {
        try { return image.ToBitmap().ConvertToAvaloniaBitmap(); }
        catch { return null; }
    }

    public static async Task<Stream?> GetStreamFromWeb(string url)
    {
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch { return null; }
        return null;
    }

    public static bool Optimize(FileInfo file)
        => optimizer.LosslessCompress(file);

    public static void SelectInExplorer(string path)
        => Process.Start("explorer.exe", $@"/select,""{path}""");

    public static void OpenInExplorer(string path)
        => Process.Start("explorer.exe", $@"""{path}""");

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