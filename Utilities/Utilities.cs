using Avalonia.Media.Imaging;
using ImageMagick;
using System;
using System.Drawing.Imaging;
using System.IO;

namespace ImagePlastic.Utilities;

public static class Utils
{
    public static int SeekIndex(int current, int offset, int total)
        => current + offset >= total ? current + offset - total
              : current + offset < 0 ? current + offset + total
                                     : current + offset;

    //Convert any image to a Bitmap, not the perfect way though.
    //Could not convert images with alpha channel correctly.
    public static Bitmap? ConvertImage(FileInfo file)
    {
        try
        {
            using MagickImage image = new(file);
            using var sysBitmap = image.ToBitmap();
            using MemoryStream stream = new();
            sysBitmap.Save(stream, ImageFormat.Bmp);
            stream.Position = 0;
            return new Bitmap(stream);
        }
        catch { return null; }
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
        if (l >= 30 && l < 40)
            return $"{double.Round(length / (double)(1 << 30), 2)} GiB";
        if (l >= 40)
            return $"{double.Round(length / (double)(1 << 40), 2)} TiB";
        return length.ToString();
    }
}