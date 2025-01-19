using Avalonia.Controls.PanAndZoom;
using Avalonia.Media;
using DynamicData;
using ExCSS;
using ImageMagick;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
        GoLeft = ReactiveCommand.Create(() => { ShowLocalImage(offset: -1); });
        GoRight = ReactiveCommand.Create(() => { ShowLocalImage(offset: 1); });
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
        DeleteCommand = ReactiveCommand.Create(async () =>
        {
            var file = Stats.File;
            if (Stats == null || Stats.IsWeb == true || file == null) return;
            var fallbackFile = SeekFile(-1);

            try
            {
                FileSystem.DeleteFile(file.FullName, Config.DeleteConfirmation ? UIOption.AllDialogs : UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                ImageFile = fallbackFile;
                ShowLocalImage();
                UIMessage = $"{file.FullName} is sent to recycle bin.";
            }
            catch (Exception e)
            {
                UIMessage = e.Message;
            }
        });
        EditCommand = ReactiveCommand.Create(() =>
        {
            if (Stats != null && Stats.EditCmd != null)
                Process.Start(Stats.EditCmd);
        });
        ShowInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (Stats != null && Stats.File != null)
                Utils.SelectInExplorer(Stats.File.FullName);
        });
        RenameCommand = ReactiveCommand.Create(async () =>
        {
            var file = Stats.File;
            if (Stats == null || Stats.IsWeb == true || file == null) return;
            var newName = await InquiryString.Handle(new("Renaming File", file.Name));
            if (string.IsNullOrEmpty(newName) || newName == file.Name) return;

            try
            {
                FileSystem.RenameFile(file.FullName, newName);

                var newFile = new FileInfo($@"{file.DirectoryName}\{newName}");
                if (newFile.Exists)
                {
                    ImageFile = newFile;
                    ShowLocalImage();
                }
            }
            catch (Exception e)
            {
                UIMessage = e.Message;
            }
        });
    }
    //For previewer.
    public MainWindowViewModel()
    {
        GoLeft = ReactiveCommand.Create(() => { });
        GoRight = ReactiveCommand.Create(() => { });
        OptCommand = ReactiveCommand.Create(() => { });
        DeleteCommand = ReactiveCommand.Create(() => { });
        EditCommand = ReactiveCommand.Create(() => { });
        ShowInExplorerCommand = ReactiveCommand.Create(() => { });
        RenameCommand = ReactiveCommand.Create(() => { });
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
    private bool pinned = false;
    public bool loading = false;
    private string? svgPath;

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
    public string? SvgPath { get => svgPath; set => this.RaiseAndSetIfChanged(ref svgPath, value); }

    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }
    public ICommand OptCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand ShowInExplorerCommand { get; }
    public ICommand RenameCommand { get; }
    public Interaction<ConfirmationWindowViewModel, bool> RequireConfirmation { get; } = new();
    public Interaction<StringInquiryWindowViewModel, string> InquiryString { get; } = new();

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };

    public void ChangeImageToPath()
    {
        Path = Path.Trim('"');
        if (string.IsNullOrEmpty(Path)) return;
        //Using index of current dir.
        else if (int.TryParse(Path, out int des))
            ShowLocalImage(destination: des - 1);
        //Using web URL.
        else if (new UrlAttribute().IsValid(Path))
            ShowWebImage(Path);
        //Using file path.
        else if (new FileInfo(Path).Exists)
        {
            ImageFile = new FileInfo(Path);
            if (ImageFile.Exists && config.Extensions.Contains(ImageFile.Extension.ToLower()))
            {
                Path = ImageFile.FullName;
                ShowLocalImage();
                return;
            }//Exceptions.
            else
            {
                Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
                ErrorReport(Stats);
            }
        }
        else
        {
            Stats = new(false);
            ErrorReport(Stats);
        }
    }

    public void Select(int offset)
    {
        if (ImageFile == null || !ImageFile.Exists || Stats.IsWeb) return;
        var currentIndex = Stats.FileIndex ?? currentDir.IndexOf(ImageFile, EqualityComparer<FileInfo>.Create((a, b) => a!.FullName.Equals(b!.FullName, StringComparison.OrdinalIgnoreCase)));
        var destination = Utils.SeekIndex(currentIndex, offset, currentDir.Count());
        var file = currentDir.ElementAt(destination);
        ImageFile = file;
        Path = file.FullName;
        Stats = new(true) { FileIndex = destination, FileCount = currentDir.Count(), File = file, DisplayName = file.Name };
    }
    public FileInfo? SeekFile(int offset)
    {
        if (ImageFile == null || !ImageFile.Exists || Stats.IsWeb) return null;
        var currentIndex = Stats.FileIndex ?? currentDir.IndexOf(ImageFile, EqualityComparer<FileInfo>.Create((a, b) => a!.FullName.Equals(b!.FullName, StringComparison.OrdinalIgnoreCase)));
        var destination = Utils.SeekIndex(currentIndex, offset, currentDir.Count());
        return currentDir.ElementAt(destination);
    }
    //Scan the path directory and show the image.
    public void ShowLocalImage(int offset = 0, int? destination = null)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        try
        {
            var files = ImageFile.Directory!.EnumerateFiles()
                                            .Where(file => config.Extensions.Contains(file.Extension.ToLower()))
                                            .OrderBy(file => file.FullName, new IntuitiveStringComparer());
            currentDir = files;
            var currentIndex = files.IndexOf(ImageFile, EqualityComparer<FileInfo>.Create((a, b) => a!.FullName.Equals(b!.FullName, StringComparison.OrdinalIgnoreCase)));
            destination ??= Utils.SeekIndex(currentIndex, offset, files.Count());
            var file = files.ElementAt((int)destination);
            ImageFile = file; Path = file.FullName;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = file, DisplayName = file.Name };

            ShowImage(file.OpenRead(), file.FullName);

            Stats = new(Stats.Success, Stats) { EditCmd = GetProcessStartInfo(file, Stats.Format) };

            if (config.Preload && file.FullName == Path)
                _ = Task.Run(() => { PreloadImage(files, file, (int)destination); });
        }
        catch
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
        }
    }
    public void PreloadImage(IEnumerable<FileInfo> files, FileInfo file, int index)
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
            currentLoads.Remove(files.ElementAt(inRangeIndex).FullName);
        }
        foreach (var ItemForRemoval in currentLoads)
            Preload.Remove(ItemForRemoval);

        //Preload Bitmaps.
        var additionOffset = leftRange - 1;
        while (++additionOffset <= rightRange && file.FullName == Path)
        {
            if (additionOffset == 0) continue;
            var preloadIndex = Utils.SeekIndex(index, additionOffset, files.Count());
            var preloadFileName = files.ElementAt(preloadIndex).FullName;
            if (Preload.ContainsKey(preloadFileName)) continue;
            if (Preload.TryAdd(preloadFileName, null))
                Task.Run(() => { Preload[preloadFileName] = Utils.ConvertImage(files.ElementAt(preloadIndex)); });
        }
    }
    public async void ShowImage(Stream stream, string path)
    {
        Loading = true;
        using MagickImage image = new(stream);
        Stats = new(true, Stats) { Format = image.Format };
        if (image.Format == MagickFormat.Svg)
        {
            SvgPath = path;
            Bitmap = null;
            Stats = new(true, Stats) { Height = image.Width, Width = image.Height };
        }
        else
        {
            IImage? bitmapTemp = null;
            if (config.Preload)
                Preload.TryGetValue(path, out bitmapTemp);
            bitmapTemp ??= await Task.Run(() => { return Utils.ConvertImage(image); });

            if (path != Path) return;
            else Bitmap = bitmapTemp;

            SvgPath = null;
            Stats = (Bitmap == null) ? new(false, Stats)
                                     : new(true, Stats) { Height = Bitmap.Size.Height, Width = Bitmap.Size.Width };
        }
        ErrorReport(Stats);
        Loading = false;
        stream.Dispose();

        //GC.Collect();
        //UIMessage = $"Estimated bytes on heap: {GC.GetTotalMemory(false)}";
    }
    public async void ShowWebImage(string url)
    {
        Loading = true; ImageFile = null;
        var webStream = await Utils.GetStreamFromWeb(url);
        if (webStream == null) return;
        Stats = new(true) { IsWeb = true };
        ShowImage(webStream, url);
        Stats = new(Stats.Success, Stats) { DisplayName = url.Split('/')[^1], Url = url };
        ErrorReport(Stats);
        webStream.Dispose();
        Loading = false;
    }
    public ProcessStartInfo? GetProcessStartInfo(FileInfo file, MagickFormat format)
    {
        if (Config.EditApp.TryGetValue(format, out string? app))
            return app != "" ? new ProcessStartInfo
            { FileName = app ?? Config.EditApp[default], Arguments = $"\"{file.FullName}\"" } : null;
        else
            return new ProcessStartInfo
            { FileName = Config.EditApp[default], Arguments = $"\"{file.FullName}\"" };
    }
}
