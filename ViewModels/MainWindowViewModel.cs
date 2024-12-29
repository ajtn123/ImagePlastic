using Avalonia;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using DynamicData;
using ImageMagick;
using ImagePlastic.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
        else
            Stats = new(true);
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
    private StretchMode stretch;

    public string[]? Args { get; }
    public Dictionary<string, Bitmap?> Preload { get; set; } = [];
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public Bitmap? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get => imageFile; set => this.RaiseAndSetIfChanged(ref imageFile, value); }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public Stats Stats { get => stats; set => this.RaiseAndSetIfChanged(ref stats, value); }
    public StretchMode Stretch { get => stretch; set => this.RaiseAndSetIfChanged(ref stretch, value); }

    public ICommand GoPath { get; }
    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };

    //Convert any image to a Bitmap, not the perfect way though.
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

    public void ChangeImageToPath()
    {
        Path = Path.Trim('"');
        if (string.IsNullOrEmpty(Path)) return;
        ImageFile = new FileInfo(Path);
        if (!ImageFile.Exists)
        {
            Stats = new(false) { File = ImageFile };
            ErrorReport(Stats);
            return;
        }
        Path = ImageFile.FullName;
        RefreshImage(0);
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
            ImageFile = files.ElementAt(destination);
            Path = fileNames.ElementAt(destination);
            var path = Path;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = ImageFile };

            await Task.Run(() =>
            {
                Bitmap? b;
                if (config.Preload)
                {
                    var searchResult = Preload.TryGetValue(Path, out Bitmap? value);
                    if (value == null)
                        searchResult = false;
                    b = searchResult ? value : ConvertImage(ImageFile);
                }
                else
                {
                    b = ConvertImage(ImageFile);
                }
                if (path == Path)
                    Bitmap = b;
            });
            if (path != Path) return;
            if (Bitmap == null)
            {
                Stats = new(false, Stats);
                ErrorReport(Stats);
            }
            else
            {
                Stats = new(true, Stats) { ImageDimension = Bitmap!.Size };
                ErrorReport(Stats);
            }

            //Todo: Improve Preload Order.
            //      ... @4 @3 # @1 @2 ...
            //Preload(🤪 ? Bitmaps : Bugs).
            if (config.Preload && path == Path)
                await Task.Run(() =>
                {
                    //Remove bitmap from preload if the image is out of preload range.
                    //It seems to be fine, hope no bugs to be discovered.
                    var loads = Preload.Keys.ToList();
                    int l = config.PreloadLeft - 1;
                    while (path == Path && l++ < Config.PreloadRight)
                    {
                        if (l == 0) continue;
                        var preIndex = destination + l >= files.Count() ? destination + l - files.Count()
                                                  : destination + l < 0 ? destination + l + files.Count()
                                                                        : destination + l;
                        loads.Remove(fileNames.ElementAt(preIndex));
                    }
                    foreach (var fn in loads)
                        Preload.Remove(fn);

                    //Preload Bitmaps.
                    int i = config.PreloadLeft - 1;
                    while (path == Path && i++ < Config.PreloadRight)
                    {
                        if (i == 0) continue;
                        var preIndex = destination + i >= files.Count() ? destination + i - files.Count()
                                                  : destination + i < 0 ? destination + i + files.Count()
                                                                        : destination + i;
                        var fn = fileNames.ElementAt(preIndex);
                        if (Preload.ContainsKey(fn)) continue;
                        if (Preload.TryAdd(fn, null))
                        {
                            var preBitmap = ConvertImage(files.ElementAt(preIndex));
                            if (preBitmap == null)
                                Preload.Remove(fn);
                            else if (Preload.ContainsKey(fn))
                                Preload[fn] = preBitmap;
                        }
                    }
                });
        }
        catch
        {
            Stats = new(false) { File = ImageFile };
            ErrorReport(Stats);
        }
    }
}

//Actually necessary, doesn't it?
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
