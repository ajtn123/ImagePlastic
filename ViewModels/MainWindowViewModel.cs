using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using DynamicData;
using ImageMagick;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
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
        Recursive = Config.RecursiveSearch;
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
        DeleteCommand = ReactiveCommand.Create(() =>
        {
            var file = Stats.File;
            if (Stats == null || Stats.IsWeb == true || file == null) return;
            var fallbackFile = SeekFile(offset: -1);

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
                Utils.SelectInExplorer(Stats.File);
        });
        RenameCommand = ReactiveCommand.Create(async () =>
        {
            var file = Stats.File;
            if (Stats == null || Stats.IsWeb == true || file == null) return;

            var newFileName = await InquiryRenameString.Handle(new(file) { Config = Config });

            if (string.IsNullOrEmpty(newFileName)) return;
            var newFile = new FileInfo(newFileName);
            if (!newFile.Exists) return;

            Stats = new(true) { File = newFile };
            Select(offset: 0);
            UIMessage = $"{file.FullName} => {newFile.Name}";
        });
        QuitCommand = ReactiveCommand.Create(() => { });
        OpenLocalCommand = ReactiveCommand.Create(async () =>
        {
            var fileUri = await OpenFilePicker.Handle(new());
            recursiveDir = null;
            LoadFile(new(fileUri.LocalPath));
        });
        OpenUriCommand = ReactiveCommand.Create(async () =>
        {
            var uriString = await InquiryUriString.Handle(new());
            if (string.IsNullOrWhiteSpace(uriString)) return;
            Path = uriString;
            ChangeImageToPath();
        });
        ReloadDirCommand = ReactiveCommand.Create(() =>
        {
            if (!Stats.IsWeb && ImageFile != null)
                LoadFile(ImageFile);
            else if (Stats.IsWeb && Stats.Url != null)
                ShowWebImage(Stats.Url);
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
        QuitCommand = ReactiveCommand.Create(() => { });
        OpenLocalCommand = ReactiveCommand.Create(() => { });
        OpenUriCommand = ReactiveCommand.Create(() => { });
        ReloadDirCommand = ReactiveCommand.Create(() => { });
    }

    //Generating a new default configuration every time.
    //A helper is needed to persist config, also a setting view.
    private Config config = new();
    private Bitmap? bitmap;
    private string path = "";
    private Stats stats = new(true) { DisplayName = "None" };
    private StretchMode stretch;
    private string? uIMessage;
    private DirectoryInfo? currentDir;
    private DirectoryInfo? recursiveDir = null;
    private bool pinned = false;
    public bool loading = false;
    private string? svgPath;
    private bool recursive;

    public string[]? Args { get; }
    public Dictionary<string, Bitmap?> Preload { get; set; } = [];
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public Bitmap? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get; set; }
    public IOrderedEnumerable<FileInfo>? CurrentDirItems { get; set; }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public Stats Stats { get => stats; set => this.RaiseAndSetIfChanged(ref stats, value); }
    public StretchMode Stretch { get => stretch; set => this.RaiseAndSetIfChanged(ref stretch, value); }
    public bool Loading { get => config.LoadingIndicator && loading; set => this.RaiseAndSetIfChanged(ref loading, value); }
    public string? UIMessage { get => uIMessage; set => this.RaiseAndSetIfChanged(ref uIMessage, value); }
    public bool Pinned { get => pinned; set => this.RaiseAndSetIfChanged(ref pinned, value); }
    public string? SvgPath { get => svgPath; set => this.RaiseAndSetIfChanged(ref svgPath, value); }
    //Re-scan dir when Recursive property changed.
    public bool Recursive
    {
        get => recursive; set
        {
            this.RaiseAndSetIfChanged(ref recursive, value);
            if (value && ImageFile != null)
                recursiveDir = ImageFile.Directory;
            else if (!value)
                recursiveDir = null;
            CurrentDirItems = null;
            if (ImageFile != null)
                LoadFile(ImageFile);
        }
    }

    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }
    public ICommand OptCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand ShowInExplorerCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand OpenLocalCommand { get; }
    public ICommand OpenUriCommand { get; }
    public ICommand ReloadDirCommand { get; }
    public ReactiveCommand<Unit, Unit> QuitCommand { get; }
    public Interaction<ConfirmationWindowViewModel, bool> RequireConfirmation { get; } = new();
    public Interaction<RenameWindowViewModel, string?> InquiryRenameString { get; } = new();
    public Interaction<Unit, string?> InquiryUriString { get; } = new();
    public Interaction<Unit, Uri?> OpenFilePicker { get; } = new();

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
            recursiveDir = null;
            LoadFile(new(Path));
        }
        else
        {
            Stats = new(false);
            ErrorReport(Stats);
        }
    }
    //Set ImageFile and load its directory.
    public void LoadFile(FileInfo file)
    {
        ImageFile = file;
        if (ImageFile.Exists && config.Extensions.Contains(ImageFile.Extension.ToLower()))
        {
            if (Recursive && (recursiveDir != null || ImageFile.Directory != null))
            {
                recursiveDir ??= ImageFile.Directory;
                LoadDir(recursiveDir!);
            }
            else if (ImageFile.Directory != null)
                LoadDir(ImageFile.Directory);
            else
            {
                IEnumerable<FileInfo> a = [ImageFile];
                CurrentDirItems = a.OrderBy(_ => 1);
            }
            ShowLocalImage();
        }
        else
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
        }
    }
    //Load when dir changed.
    public void LoadDir(DirectoryInfo dir)
    {
        if (CurrentDirItems != null && currentDir != null && currentDir.FullName == dir.FullName) return;
        currentDir = dir;
        CurrentDirItems = dir!.EnumerateFiles("", Recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly)
                              .Where(file => Config.Extensions.Contains(file.Extension.ToLower()))
                              .OrderBy(file => file.FullName, new IntuitiveStringComparer());
    }
    //Load and show info of ImageFile or its neighbor without decode or rendering.
    public void Select(int offset = 0, int? destination = null)
    {
        if (Stats == null || Stats.File == null || !Stats.File.Exists || Stats.IsWeb || CurrentDirItems == null) return;
        destination ??= Utils.SeekIndex(GetCurrentIndex(), offset, CurrentDirItems.Count());
        var file = CurrentDirItems.ElementAt((int)destination);
        ImageFile = file;
        Path = file.FullName;
        Stats = new(true, offset == 0 ? Stats : null) { FileIndex = destination, FileCount = CurrentDirItems!.Count(), File = file, DisplayName = file.Name };
    }
    //Return FileInfo of ImageFile or its neighbor.
    public FileInfo? SeekFile(int offset = 0, int? destination = null)
    {
        if (Stats == null || Stats.File == null || !Stats.File.Exists || Stats.IsWeb || CurrentDirItems == null) return null;
        destination ??= Utils.SeekIndex(GetCurrentIndex(), offset, CurrentDirItems.Count());
        return CurrentDirItems.ElementAt((int)destination);
    }
    //Show ImageFile or its neighbor.
    public async void ShowLocalImage(int offset = 0, int? destination = null)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        try
        {
            var files = CurrentDirItems;
            if (files == null || !files.Any()) return;

            destination ??= Utils.SeekIndex(GetCurrentIndex(), offset, files.Count());
            var file = files.ElementAt((int)destination);
            ImageFile = file; Path = file.FullName;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = file, DisplayName = file.Name };

            using (var fs = file.OpenRead())
                await ShowImage(fs, file.FullName);

            Stats = new(Stats.Success, Stats) { EditCmd = GetEditAppStartInfo(file, Stats.Format) };

            if (config.Preload && file.FullName == Path)
                PreloadImage(files, (int)destination);
        }
        catch
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
        }
    }
    //Show image from the string, use path as identifier.
    public async Task ShowImage(Stream stream, string path)
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
            Bitmap? bitmapTemp = null;
            if (config.Preload)
                Preload.TryGetValue(path, out bitmapTemp);
            bitmapTemp ??= await Task.Run(() => { return Utils.ConvertImage(image); });

            if (path != Path) return;
            Bitmap = bitmapTemp;

            SvgPath = null;
            Stats = (Bitmap == null) ? new(false, Stats)
                                     : new(true, Stats) { Height = Bitmap.Size.Height, Width = Bitmap.Size.Width };
        }
        ErrorReport(Stats);
        Loading = false;

        //GC.Collect();
    }
    public async void ShowWebImage(string url)
    {
        Loading = true; ImageFile = null;
        using var webStream = await Utils.GetStreamFromWeb(url);
        if (webStream == null)
        {
            Stats = new(false) { IsWeb = true, Url = url };
        }
        else
        {
            Stats = new(true) { IsWeb = true, Url = url };
            await ShowImage(webStream, url);
        }
        Stats = new(Stats.Success, Stats) { DisplayName = url.Split('/')[^1] };
        ErrorReport(Stats);
        Loading = false;
    }
    //Get index of current file.
    public int GetCurrentIndex()
    {
        if (Stats.FileCount == CurrentDirItems!.Count() && Stats.FileIndex != null)
            return (int)Stats.FileIndex;
        else return CurrentDirItems!.IndexOf(ImageFile, Utils.FileInfoComparer);
    }
    public ProcessStartInfo? GetEditAppStartInfo(FileInfo file, MagickFormat format)
    {
        if (Config.EditApp.TryGetValue(format, out string? app))
            return app != "" ? new ProcessStartInfo
            { FileName = app ?? Config.EditApp[default], Arguments = $"\"{file.FullName}\"" } : null;
        else
            return new ProcessStartInfo
            { FileName = Config.EditApp[default], Arguments = $"\"{file.FullName}\"" };
    }

    private CancellationTokenSource? preloadCTS = null;
    private readonly Lock preloadLock = new();
    public void PreloadImage(IEnumerable<FileInfo> files, int index)
    {
        // Cancel previous preloading tasks
        lock (preloadLock)
        {
            preloadCTS?.Cancel();
            preloadCTS = new CancellationTokenSource();
        }

        var token = preloadCTS.Token;
        var preloadTasks = new ConcurrentDictionary<string, Task>();

        // Define preload range
        int leftRange = Math.Max(-config.PreloadLeft, -(files.Count() - 1));
        int rightRange = Math.Min(config.PreloadRight, files.Count() - 1);

        // Identify files to preload
        var newPreloadSet = new HashSet<string>();
        foreach (var offset in Enumerable.Range(leftRange, rightRange - leftRange + 1))
        {
            if (offset == 0) continue;

            int preloadIndex = Utils.SeekIndex(index, offset, files.Count());
            var preloadFileName = files.ElementAt(preloadIndex).FullName;
            newPreloadSet.Add(preloadFileName);

            if (!Preload.ContainsKey(preloadFileName))
            {
                Preload.TryAdd(preloadFileName, null);
                preloadTasks.TryAdd(preloadFileName, Task.Run(() =>
                {
                    if (token.IsCancellationRequested) return;
                    var bitmap = Utils.ConvertImage(files.ElementAt(preloadIndex));
                    if (!token.IsCancellationRequested)
                        Preload[preloadFileName] = bitmap;
                }, token));
            }
        }

        // Remove preloads outside the range
        var keysToRemove = Preload.Keys.Where(key => !newPreloadSet.Contains(key)).ToList();
        foreach (var key in keysToRemove)
            Preload.Remove(key, out _);

        // Wait for tasks to complete (optional, for debugging)
        Task.WhenAll(preloadTasks.Values).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
                GC.Collect();
        }, TaskScheduler.Default);
    }
}
