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
        GoLeft = ReactiveCommand.Create(() => { RefreshImage(offset: -1); });
        GoRight = ReactiveCommand.Create(() => { RefreshImage(offset: 1); });
        OptCommand = ReactiveCommand.Create(async () =>
        {
            if (Stats == null || Stats.Optimizable == false) return;
            Loading = true;
            var beforeLength = Utils.ToReadable(Stats.File!.Length);
            var result = await Task.Run(() => { return Utils.Optimize(Stats.File!); });
            Loading = false;
            Stats = new(true, Stats);
            UIMessage = $"Opt: {Stats.DisplayName} {result}" + (result ? $"{beforeLength} => {Utils.ToReadable(Stats.File!.Length)}" : "");
        });
    }
    //For previewer.
    public MainWindowViewModel()
    {
        GoLeft = ReactiveCommand.Create(() => { });
        GoRight = ReactiveCommand.Create(() => { });
        OptCommand = ReactiveCommand.Create(() => { });
    }

    //Generating a new default configuration every time.
    //A helper is needed to persist config, also a setting view.
    private Config config = new();
    private IImage? bitmap;
    private string path = "";
    private Stats stats = new(true) { DisplayName = "None" };
    private StretchMode stretch;
    private string? uIMessage;
    private IEnumerable<FileInfo> currentDir = [];
    private IEnumerable<string> currentDirName = [];
    private bool pinned = false;
    public bool loading = false;

    public string[]? Args { get; }
    public Dictionary<string, IImage?> Preload { get; set; } = [];
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public IImage? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get; set; }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public Stats Stats { get => stats; set => this.RaiseAndSetIfChanged(ref stats, value); }
    public StretchMode Stretch { get => stretch; set => this.RaiseAndSetIfChanged(ref stretch, value); }
    public bool Loading { get => config.LoadingIndicator && loading; set => this.RaiseAndSetIfChanged(ref loading, value); }
    public string? UIMessage { get => uIMessage; set => this.RaiseAndSetIfChanged(ref uIMessage, value); }
    public bool Pinned { get => pinned; set => this.RaiseAndSetIfChanged(ref pinned, value); }

    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }
    public ICommand OptCommand { get; }

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };

    public void ChangeImageToPath()
    {
        Path = Path.Trim('"');
        if (string.IsNullOrEmpty(Path)) return;

        //Using index of current dir.
        if (int.TryParse(Path, out int des))
        {
            RefreshImage(destination: des - 1);
            return;
        }

        //Using web URL.
        if (new UrlAttribute().IsValid(Path))
        {
            ShowWebImage(Path);
            return;
        }

        //Using file path.
        ImageFile = new FileInfo(Path);
        if (ImageFile.Exists && config.Extensions.Contains(ImageFile.Extension.ToLower()))
        {
            Path = ImageFile.FullName;
            RefreshImage();
            return;
        }

        //Exceptions.
        Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
        ErrorReport(Stats);
    }

    public void Select(int offset)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        var currentIndex = Stats.FileIndex ?? currentDirName.IndexOf(ImageFile.FullName);
        var destination = Utils.SeekIndex(currentIndex, offset, currentDir.Count());
        var file = currentDir.ElementAt(destination);
        ImageFile = file;
        Path = file.FullName;
        Stats = new(true) { FileIndex = destination, FileCount = currentDir.Count(), File = file, DisplayName = file.Name };
    }
    //Scan the path directory and show the image.
    public void RefreshImage(int offset = 0, int? destination = null)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        try
        {
            var files = ImageFile.Directory!.EnumerateFiles().Where(a => config.Extensions.Contains(a.Extension.ToLower()));
            var fileNames = files.Select(a => a.FullName);
            currentDir = files; currentDirName = fileNames;
            var currentIndex = fileNames.IndexOf(ImageFile.FullName);
            destination ??= Utils.SeekIndex(currentIndex, offset, files.Count());
            var file = files.ElementAt((int)destination);
            ImageFile = file;
            Path = file.FullName;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = file, DisplayName = file.Name };

            ShowLocalImage(file);

            if (config.Preload && file.FullName == Path)
                _ = Task.Run(() => { PreloadImage(files, fileNames, file, (int)destination); });
        }
        catch
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
        }
    }
    public void PreloadImage(IEnumerable<FileInfo> files, IEnumerable<string> fileNames, FileInfo file, int index)
    {
        var leftRange = (-config.PreloadLeft < files.Count()) ? config.PreloadLeft : -(files.Count() - 1);
        var rightRange = (config.PreloadRight < files.Count()) ? config.PreloadRight : (files.Count() - 1);

        //Remove bitmap from preload if the image is out of preload range.
        //It seems to be fine, hope no bugs to be discovered.
        var currentLoads = Preload.Keys.ToList();
        var removalOffset = leftRange - 1;
        while (++removalOffset <= rightRange && file.FullName == Path)
        {
            var inRangeIndex = Utils.SeekIndex(index, removalOffset, files.Count());
            currentLoads.Remove(fileNames.ElementAt(inRangeIndex));
        }
        foreach (var ItemForRemoval in currentLoads)
            Preload.Remove(ItemForRemoval);

        //Preload Bitmaps.
        var additionOffset = leftRange - 1;
        while (++additionOffset <= rightRange && file.FullName == Path)
        {
            if (additionOffset == 0) continue;
            var preloadIndex = Utils.SeekIndex(index, additionOffset, files.Count());
            var preloadFileName = fileNames.ElementAt(preloadIndex);
            if (Preload.ContainsKey(preloadFileName)) continue;
            if (Preload.TryAdd(preloadFileName, null))
                Task.Run(() => { Preload[preloadFileName] = Utils.ConvertImage(files.ElementAt(preloadIndex)); });
        }
    }
    public async void ShowLocalImage(FileInfo file)
    {
        Loading = true;
        IImage? bitmapTemp = null;
        if (config.Preload)
            Preload.TryGetValue(file.FullName, out bitmapTemp);
        bitmapTemp ??= await Task.Run(() => { return Utils.ConvertImage(file); });

        if (file.FullName != Path) return;
        else Bitmap = bitmapTemp;

        Loading = false;
        Stats = (Bitmap == null) ? new(false, Stats)
                                 : new(true, Stats) { ImageDimension = Bitmap!.Size };
        ErrorReport(Stats);

        //GC.Collect();
        //UIMessage = $"Estimated bytes on heap: {GC.GetTotalMemory(false)}";
    }
    public async void ShowWebImage(string url)
    {
        Loading = true;
        var bitmapTemp = await Utils.ConvertImageFromWeb(url);
        if (url == Path) { Bitmap = bitmapTemp; }
        Stats = (Bitmap == null) ? new(false) { IsWeb = true, DisplayName = url.Split('/')[^1] }
                                 : new(true) { IsWeb = true, DisplayName = url.Split('/')[^1], ImageDimension = Bitmap!.Size };
        ErrorReport(Stats);
        Loading = false;
    }
}
