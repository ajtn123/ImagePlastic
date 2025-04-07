using Avalonia.Media.Imaging;
using ImagePlastic.Utilities;
using System.IO;
using System.Threading.Tasks;

namespace ImagePlastic.Models;

public class ThumbnailItem
{
    public required FileInfo File { get; set; }
    public Task<Bitmap?> BitmapAsync => Task.Run(() => Utils.GetThumbnail(File.FullName));
}
