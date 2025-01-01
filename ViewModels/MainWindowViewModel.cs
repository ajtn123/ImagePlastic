using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;
using DynamicData;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            Path = Config.DefaultFile.FullName;
        if (args != null && args.Length > 0)
            Path = args[0];
        else
            Stats = new(true);
        Stretch = Config.Stretch;
        GoPath = ReactiveCommand.Create(ChangeImageToPath);
        GoLeft = ReactiveCommand.Create(() => { RefreshImage(offset: -1); });
        GoRight = ReactiveCommand.Create(() => { RefreshImage(offset: 1); });
    }
    //For previewer.
    public MainWindowViewModel()
    {
        GoPath = ReactiveCommand.Create(() => { });
        GoLeft = ReactiveCommand.Create(() => { });
        GoRight = ReactiveCommand.Create(() => { });
    }

    //Generating a new default configuration every time.
    //A helper is needed to persist config, also a setting view.
    private Config config = new();
    private IImage? bitmap;
    private string path = "";
    private Stats stats = new(true) { DisplayName = "None" };
    private StretchMode stretch;
    private bool loading = false;

    public string[]? Args { get; }
    public Dictionary<string, IImage?> Preload { get; set; } = [];
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public IImage? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get; set; }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public Stats Stats { get => stats; set => this.RaiseAndSetIfChanged(ref stats, value); }
    public StretchMode Stretch { get => stretch; set => this.RaiseAndSetIfChanged(ref stretch, value); }
    public bool Loading { get => config.LoadingIndicator && loading; set => this.RaiseAndSetIfChanged(ref loading, value); }

    public ICommand GoPath { get; }
    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };

    public void ChangeImageToPath()
    {
        Path = Path.Trim('"');
        if (string.IsNullOrEmpty(Path)) return;

        //Definitely should not be here.
        if (int.TryParse(Path, out int des))
        {
            RefreshImage(destination: des - 1);
            return;
        }

        if (new UrlAttribute().IsValid(Path))
        {
            ShowWebImage(Path);
            return;
        }

        ImageFile = new FileInfo(Path);
        if (!ImageFile.Exists || !config.Extensions.Contains(ImageFile.Extension.ToLower()))
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
            return;
        }
        Path = ImageFile.FullName;
        RefreshImage();
    }

    //Scan the path directory and show the image.
    public async void RefreshImage(int offset = 0, int? destination = null)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        try
        {
            Loading = true;
            var files = ImageFile.Directory!.EnumerateFiles().Where(a => config.Extensions.Contains(a.Extension.ToLower()));
            var fileNames = files.Select(a => a.FullName);
            var currentIndex = fileNames.IndexOf(ImageFile.FullName);
            destination ??= Utils.SeekIndex(currentIndex, offset, files.Count());
            var file = files.ElementAt((int)destination);
            ImageFile = file;
            Path = file.FullName;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = file, DisplayName = file.Name };

            IImage? bitmapTemp = null;
            if (config.Preload)
                Preload.TryGetValue(file.FullName, out bitmapTemp);
            bitmapTemp ??= await Task.Run(() => { return Utils.ConvertImage(file); });

            if (file.FullName != Path) return;
            else
            {
                Bitmap = bitmapTemp;
                Loading = false;
            }

            Stats = (Bitmap == null) ? new(false, Stats)
                                     : new(true, Stats) { ImageDimension = Bitmap!.Size };
            ErrorReport(Stats);

            //This has messed up the whole thing!!!
            if (config.Preload && file.FullName == Path)
                await Task.Run(() =>
                {
                    var leftRange = (-config.PreloadLeft < files.Count()) ? config.PreloadLeft : -(files.Count() - 1);
                    var rightRange = (config.PreloadRight < files.Count()) ? config.PreloadRight : (files.Count() - 1);

                    //Remove bitmap from preload if the image is out of preload range.
                    //It seems to be fine, hope no bugs to be discovered.
                    var currentLoads = Preload.Keys.ToList();
                    var removalOffset = leftRange - 1;
                    while (++removalOffset <= rightRange && file.FullName == Path)
                    {
                        var inRangeIndex = Utils.SeekIndex((int)destination, removalOffset, files.Count());
                        currentLoads.Remove(fileNames.ElementAt(inRangeIndex));
                    }
                    foreach (var ItemForRemoval in currentLoads)
                        Preload.Remove(ItemForRemoval);

                    //Preload Bitmaps.
                    var additionOffset = leftRange - 1;
                    while (++additionOffset <= rightRange && file.FullName == Path)
                    {
                        if (additionOffset == 0) continue;
                        var preloadIndex = Utils.SeekIndex((int)destination, additionOffset, files.Count());
                        var preloadFileName = fileNames.ElementAt(preloadIndex);
                        if (Preload.ContainsKey(preloadFileName)) continue;
                        if (Preload.TryAdd(preloadFileName, null))
                            Preload[preloadFileName] = Utils.ConvertImage(files.ElementAt(preloadIndex));
                    }
                });
        }
        catch
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
        }
    }
    public void PreloadImage(FileInfo file)
    {
        //Later... in a month probably.
        throw new NotImplementedException();
    }
    public async void ShowLocalImage(FileInfo file)
    {
        //The Huge Method above should be separated.
        throw new NotImplementedException();
    }
    public async void ShowWebImage(string url)
    {
        Loading = true;
        var bitmapTemp = await Task.Run(() => { return Utils.ConvertImageFromWeb(url); });
        if (url == Path) { Bitmap = bitmapTemp; }
        Stats = (Bitmap == null) ? new(false) { IsWeb = true }
                                 : new(true) { IsWeb = true, DisplayName = url.Split('/')[^1], ImageDimension = Bitmap!.Size };
        ErrorReport(Stats);
        Loading = false;
    }
}
