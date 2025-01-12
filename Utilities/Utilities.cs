using Avalonia.Media;
using Avalonia.Media.Imaging;
using ImageMagick;
using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ImagePlastic.Utilities;

public static class Utils
{
    private static readonly HttpClient client = new();
    public static int SeekIndex(int current, int offset, int total)
        => current + offset >= total ? current + offset - total
              : current + offset < 0 ? current + offset + total
                                     : current + offset;

    //Convert any image to a Bitmap.
    public static IImage? ConvertImage(FileInfo file)
    {
        try
        {
            return
                new MagickImage(file).ToBitmap().ConvertToAvaloniaBitmap() ??
                SKBitmap.Decode(file.FullName).ToAvaloniaImage();
        }
        catch { return null; }
    }

    public static async Task<Bitmap?> ConvertImageFromWeb(string url)
    {
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out Uri? uri))
            try
            {
                using HttpResponseMessage response = await client.GetAsync(uri);
                response.EnsureSuccessStatusCode();
                Stream responseStream = await response.Content.ReadAsStreamAsync();
                return new MagickImage(responseStream).ToBitmap().ConvertToAvaloniaBitmap();
            }
            catch { return null; }
        return null;
    }

    public static bool Optimize(FileInfo file)
    {
        var optimizer = new ImageOptimizer();
        return optimizer.LosslessCompress(file);
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