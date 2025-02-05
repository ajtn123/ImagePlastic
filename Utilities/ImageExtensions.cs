using ImageMagick;
using System.Drawing;
using System.Drawing.Imaging;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace ImagePlastic.Utilities;

//https://github.com/AvaloniaUI/Avalonia/discussions/5908#discussioncomment-806242
public static class ImageExtensions
{
    public static Bitmap? ConvertToAvaloniaBitmap(this MagickImage magick)
    {
        using var sysBitmap = magick.ToBitmap();
        if (sysBitmap == null)
            return null;
        var bitmapData = sysBitmap.LockBits(new Rectangle(0, 0, sysBitmap.Width, sysBitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        Bitmap avaBitmap = new(
            Avalonia.Platform.PixelFormat.Bgra8888,
            Avalonia.Platform.AlphaFormat.Premul,
            bitmapData.Scan0,
            new Avalonia.PixelSize(bitmapData.Width, bitmapData.Height),
            new Avalonia.Vector(96, 96),
            bitmapData.Stride);
        sysBitmap.UnlockBits(bitmapData);
        sysBitmap.Dispose();
        return avaBitmap;
    }
}