using Avalonia;
using Avalonia.Platform;
using ImageMagick;
using System.Drawing;
using System.Drawing.Imaging;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using PixelFormat = Avalonia.Platform.PixelFormat;
using SysBitmap = System.Drawing.Bitmap;

namespace ImagePlastic.Utilities;

//https://github.com/AvaloniaUI/Avalonia/discussions/5908#discussioncomment-806242
public static class ImageExtensions
{
    public static Bitmap? ConvertToAvaloniaBitmap(this MagickImage magick)
    {
        using var sysBitmap = magick.ToBitmap();
        if (sysBitmap == null) return null;
        var bitmapData = sysBitmap.LockBits(new Rectangle(0, 0, sysBitmap.Width, sysBitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Bitmap avaBitmap = new(
            PixelFormat.Bgra8888,
            AlphaFormat.Premul,
            bitmapData.Scan0,
            new PixelSize(bitmapData.Width, bitmapData.Height),
            new Vector(96, 96),
            bitmapData.Stride);
        sysBitmap.UnlockBits(bitmapData);
        return avaBitmap;
    }
    public static Bitmap? ConvertToAvaloniaBitmap(this SysBitmap sysBitmap)
    {
        if (sysBitmap == null) return null;
        var bitmapData = sysBitmap.LockBits(new Rectangle(0, 0, sysBitmap.Width, sysBitmap.Height), ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        Bitmap avaBitmap = new(
            PixelFormat.Bgra8888,
            AlphaFormat.Premul,
            bitmapData.Scan0,
            new PixelSize(bitmapData.Width, bitmapData.Height),
            new Vector(96, 96),
            bitmapData.Stride);
        sysBitmap.UnlockBits(bitmapData);
        return avaBitmap;
    }
}
