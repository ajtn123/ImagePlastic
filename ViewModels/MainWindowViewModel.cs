using Avalonia;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using DynamicData;
using ImageMagick;
using ImagePlastic.Models;
using ReactiveUI;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(string[]? args)
    {
        Args = args;
        if (Config.DefaultFile != null)
            path = Config.DefaultFile.FullName;
        if (args != null && args.Length > 0)
            path = args[0];
        Stretch = Config.Stretch;
        ChangeImageToPath();
        GoPath = ReactiveCommand.Create(ChangeImageToPath);
        GoLeft = ReactiveCommand.Create(() => { RefreshImage(-1); });
        GoRight = ReactiveCommand.Create(() => { RefreshImage(1); });
    }
    //For previewer.
    public MainWindowViewModel()
    {
        if (Config.DefaultFile != null)
            path = Config.DefaultFile.FullName;
        ChangeImageToPath();
        GoPath = ReactiveCommand.Create(ChangeImageToPath);
        GoLeft = ReactiveCommand.Create(() => { RefreshImage(-1); });
        GoRight = ReactiveCommand.Create(() => { RefreshImage(1); });
    }

    //Generating a new default configuration every time.
    //A helper is needed to persist config, also a setting view.
    private Config config = new();
    private Bitmap? bitmap;
    private FileInfo? imageFile;
    private string path = "";
    private Stats stats = new(true);
    private int? fileIndex;
    private StretchMode stretch;

    public string[]? Args { get; }
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public Bitmap? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get => imageFile; set => this.RaiseAndSetIfChanged(ref imageFile, value); }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public int? FileIndex { get => fileIndex; set => this.RaiseAndSetIfChanged(ref fileIndex, value); }
    public Stats Stats { get => stats; set => this.RaiseAndSetIfChanged(ref stats, value); }
    public StretchMode Stretch { get => stretch; set => this.RaiseAndSetIfChanged(ref stretch, value); }

    public ICommand GoPath { get; }
    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };
    public void ReportError()
    {
        Stats = new(false) { File = ImageFile };
        ErrorReport(Stats);
    }

    //Convert any image to a Bitmap, not the perfect way though.
    public Bitmap? ConvertImage()
    {
        try
        {
            using var image = new MagickImage(ImageFile!.FullName);
            using var sysBitmap = image.ToBitmap();
            using MemoryStream stream = new();
            sysBitmap.Save(stream, ImageFormat.Bmp);
            stream.Position = 0;
            return new Bitmap(stream);
        }
        catch { ReportError(); return null; }
    }

    public void ChangeImageToPath()
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            ImageFile = new FileInfo(Path);
            if (!ImageFile.Exists)
            {
                ReportError();
                return;
            }
            Path = ImageFile.FullName;
            RefreshImage(0);
        }
        catch { return; }
    }

    //Scan the path directory and show the image.
    public async void RefreshImage(int offset = 0)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        try
        {
            var files = ImageFile.Directory!.EnumerateFiles().Where(a => config.Extensions.Contains(a.Extension.TrimStart('.').ToLower()));
            var fileNames = files.Select(a => a.FullName);
            var currentIndex = fileNames.IndexOf(ImageFile.FullName);
            var destination = currentIndex + offset >= files.Count() ? currentIndex + offset - files.Count()
                                         : currentIndex + offset < 0 ? currentIndex + offset + files.Count()
                                                                     : currentIndex + offset;
            FileIndex = destination;
            ImageFile = files.ElementAt(destination);
            Path = fileNames.ElementAt(destination);
            var path = Path;
            Stats = new(true) { FileIndex = FileIndex, FileCount = files.Count(), File = ImageFile };

            await Task.Run(() =>
            {
                var b = ConvertImage();
                if (path == Path)
                    Bitmap = b;
            });
            if (path == Path)
            {
                Stats = new(true, Stats) { ImageDimension = Bitmap!.Size };
                ErrorReport(Stats);
            }
        }
        catch { ReportError(); }
    }
}

//Not necessary, will be deleted later. Will it?
public class Stats
{
    public Stats(bool success, Stats? stats = null)
    {
        Success = success;

        if (stats == null) return;
        FileIndex = stats.FileIndex;
        ImageDimension = stats.ImageDimension;
        File = stats.File;
        FileCount = stats.FileCount;
    }
    public bool Success { get; }
    public FileInfo? File { get; set; }
    public int? FileIndex { get; set; }
    public int? FileCount { get; set; }
    public Size? ImageDimension { get; set; }
}
