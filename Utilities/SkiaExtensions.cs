using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;
using System;

namespace ImagePlastic.Utilities;

//https://github.com/AvaloniaUI/Avalonia/discussions/13610#discussioncomment-7576208
public static class SkiaExtensions
{
    private record class SKBitmapDrawOperation : ICustomDrawOperation
    {
        public Rect Bounds { get; set; }
        public SKBitmap? Bitmap { get; init; }

        public void Dispose() { }
        public bool Equals(ICustomDrawOperation? other) => false;
        public bool HitTest(Point p) => Bounds.Contains(p);
        public void Render(ImmediateDrawingContext context)
        {
            if (Bitmap is SKBitmap bitmap && context.PlatformImpl.GetFeature<ISkiaSharpApiLeaseFeature>() is ISkiaSharpApiLeaseFeature leaseFeature)
            {
                ISkiaSharpApiLease lease = leaseFeature.Lease();
                using (lease)
                {
                    lease.SkCanvas.DrawBitmap(bitmap, SKRect.Create((float)Bounds.X, (float)Bounds.Y, (float)Bounds.Width, (float)Bounds.Height));
                }
            }
        }
    }

    private class AvaloniaImage : IImage, IDisposable
    {
        private readonly SKBitmap? _source;
        private SKBitmapDrawOperation? _drawImageOperation;

        public AvaloniaImage(SKBitmap? source)
        {
            _source = source;
            if (source?.Info.Size is SKSizeI size)
                Size = new(size.Width, size.Height);
        }

        public Size Size { get; }

        public void Dispose() => _source?.Dispose();
        public void Draw(DrawingContext context, Rect sourceRect, Rect destRect)
        {
            _drawImageOperation ??= new() { Bitmap = _source };
            _drawImageOperation.Bounds = sourceRect;
            context.Custom(_drawImageOperation);
        }
    }

    public static SKBitmap? ToSKBitmap(this System.IO.Stream? stream)
    {
        if (stream == null)
            return null;
        return SKBitmap.Decode(stream);
    }
    public static IImage? ToAvaloniaImage(this SKBitmap? bitmap)
    {
        if (bitmap != null)
            return new AvaloniaImage(bitmap);
        return default;
    }
}