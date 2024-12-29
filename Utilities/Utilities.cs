using Avalonia.Media.Imaging;
using ImageMagick;
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
}