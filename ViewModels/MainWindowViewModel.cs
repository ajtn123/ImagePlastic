using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;
using DynamicData;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ReactiveUI;
using System;
using System.Collections.Generic;
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
    private IImage? bitmap;
    private FileInfo? imageFile;
    private string path = "";
    private Stats stats = new(true);
    private StretchMode stretch;

    public string[]? Args { get; }
    public Dictionary<string, IImage?> Preload { get; set; } = [];
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public IImage? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get => imageFile; set => this.RaiseAndSetIfChanged(ref imageFile, value); }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public Stats Stats { get => stats; set => this.RaiseAndSetIfChanged(ref stats, value); }
    public StretchMode Stretch { get => stretch; set => this.RaiseAndSetIfChanged(ref stretch, value); }

    public ICommand GoPath { get; }
    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };

    public void ChangeImageToPath()
    {
        Path = Path.Trim('"');
        if (string.IsNullOrEmpty(Path)) return;
        ImageFile = new FileInfo(Path);
        if (!ImageFile.Exists || !config.Extensions.Contains(ImageFile.Extension.ToLower()))
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
            var files = ImageFile.Directory!.EnumerateFiles().Where(a => config.Extensions.Contains(a.Extension.ToLower()));
            var fileNames = files.Select(a => a.FullName);
            var currentIndex = fileNames.IndexOf(ImageFile.FullName);
            var destination = Utils.SeekIndex(currentIndex, offset, files.Count());
            ImageFile = files.ElementAt(destination);
            Path = fileNames.ElementAt(destination);
            var path = Path;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = ImageFile };

            await Task.Run(() =>
            {
                IImage? b, v;
                if (config.Preload)
                    Preload.TryGetValue(Path, out v);
                else
                    v = null;
                b = v ?? Utils.ConvertImage(ImageFile);

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

            //This has messed up the whole thing!!!
            if (config.Preload && path == Path)
                await Task.Run(() =>
                {
                    var pl = (-config.PreloadLeft < files.Count()) ? config.PreloadLeft : -(files.Count() - 1);
                    var pr = (config.PreloadRight < files.Count()) ? config.PreloadRight : (files.Count() - 1);

                    //Remove bitmap from preload if the image is out of preload range.
                    //It seems to be fine, hope no bugs to be discovered.
                    var loads = Preload.Keys.ToList();
                    var l = pl - 1;
                    while (path == Path && l++ < pr)
                    {
                        var preIndex = Utils.SeekIndex(destination, l, files.Count());
                        loads.Remove(fileNames.ElementAt(preIndex));
                    }
                    foreach (var fn in loads)
                        Preload.Remove(fn);

                    //Preload Bitmaps.
                    var i = pl - 1;
                    while (path == Path && i++ < pr)
                    {
                        if (i == 0) continue;
                        var preIndex = Utils.SeekIndex(destination, i, files.Count());
                        var fn = fileNames.ElementAt(preIndex);
                        if (Preload.ContainsKey(fn)) continue;
                        if (Preload.TryAdd(fn, null))
                        {
                            var preBitmap = Utils.ConvertImage(files.ElementAt(preIndex));
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
