using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using DynamicData;
using ImageMagick;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
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
    public MainWindowViewModel()
    {
        if (Config.DefaultFile != null)
            Path = Config.DefaultFile.FullName;
        else
            Stats = new(true);
        fsWatcher = new()
        {
            Filter = "*.*",
            NotifyFilter = NotifyFilters.FileName,
        };
        //FSWatcher.Changed += OnChanged;
        fsWatcher.Created += OnFSChanged;
        fsWatcher.Deleted += OnFSChanged;
        fsWatcher.Renamed += OnFSChanged;
        Stretch = Config.Stretch;
        Recursive = Config.RecursiveSearch;
        GoPath = ReactiveCommand.Create(ChangeImageToPath);
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
            if (Config.DeleteConfirmation && !await RequireConfirmation.Handle(new ConfirmationWindowViewModel("Delete Confirmation", $"Deleting file {file.FullName}"))) return;
            var fallbackFile = SeekFile(offset: -1);

            try
            {
                FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                ImageFile = fallbackFile;
                ShowLocalImage();
                UIMessage = $"{file.FullName} is sent to recycle bin.";
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
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

            var newFilePath = await InquiryRenameString.Handle(new(file, false));

            if (string.IsNullOrEmpty(newFilePath)) return;
            var newFile = new FileInfo(newFilePath);
            if (!newFile.Exists) return;

            ImageFile = newFile;
            Select(offset: 0);
            UIMessage = $"{file.FullName} => {newFile.Name}";
        });
        MoveCommand = ReactiveCommand.Create(async () =>
        {
            var file = Stats.File;
            if (Stats == null || Stats.IsWeb == true || file == null) return;

            var newFilePath = await InquiryRenameString.Handle(new(file, true));

            if (string.IsNullOrEmpty(newFilePath)) return;
            var newFile = new FileInfo(newFilePath);
            if (!newFile.Exists) return;

            LoadFile(newFile);
            UIMessage = $"{file.FullName} => {newFile.FullName}";
        });
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
            ChangeImageToPath(uriString);
        });
        ReloadDirCommand = ReactiveCommand.Create(() =>
        {
            Preload.Clear();
            if (!Stats.IsWeb && Stats.File != null)
                LoadFile(Stats.File);
            else if (Stats.IsWeb && Stats.Url != null)
                ShowWebImage(Stats.Url);
        });
        ConfigureCommand = ReactiveCommand.Create(() =>
        {
            Config.Save();
            Process.Start("explorer", "\"IPConfig.json\"");
        });
        PickColorCommand = ReactiveCommand.Create(() =>
        {
            if (Magick == null)
                if (Stats.File != null) Magick = new(Stats.File);
                else return;
            UIMessage = Magick.GetPixels().GetPixel(100, 100).ToColor()?.ToHexString();
        });
    }

    private string[]? args;
    private DirectoryInfo? currentDir;
    private DirectoryInfo? recursiveDir = null;
    public bool loading = false;
    public bool fsChanged = false;
    private readonly FileSystemWatcher fsWatcher;
    private bool recursive;
    private Stats stats = new(true);
    private MagickImage? magick;
    public Dictionary<string, Bitmap?> Preload = [];
    public FileInfo? ImageFile;
    public IOrderedEnumerable<FileInfo>? CurrentDirItems;

    public string[]? Args
    {
        get => args; set
        {
            args = value;
            if (Args != null && Args.Length != 0)
                ChangeImageToPath(Args[0]);
        }
    }
    [Reactive]
    public Bitmap? Bitmap { get; set; }
    public MagickImage? Magick
    {
        get => magick; set
        {
            magick?.Dispose();
            this.RaiseAndSetIfChanged(ref magick, value);
        }
    }
    public string Path { get => StringInquiryViewModel.Result; set => StringInquiryViewModel.Result = value; }
    public StringInquiryViewModel StringInquiryViewModel { get; set; } = new(message: "Image Path");
    [Reactive]
    public StatsViewModel? StatsViewModel { get; set; }
    public Stats Stats { get => stats; set { stats = value; StatsViewModel = new(value); } }
    [Reactive]
    public StretchMode Stretch { get; set; }
    public bool Loading { get => Config.LoadingIndicator && loading; set => this.RaiseAndSetIfChanged(ref loading, value); }
    [Reactive]
    public string? UIMessage { get; set; }
    [Reactive]
    public bool Pinned { get; set; }
    [Reactive]
    public bool WindowOnTop { get; set; }
    [Reactive]
    public string? SvgPath { get; set; }
    //Reload dir when Recursive property changed.
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

    public ICommand GoPath { get; }
    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }
    public ICommand OptCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand EditCommand { get; }
    public ICommand ShowInExplorerCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand MoveCommand { get; }
    public ICommand OpenLocalCommand { get; }
    public ICommand OpenUriCommand { get; }
    public ICommand ReloadDirCommand { get; }
    public ICommand ConfigureCommand { get; }
    public ICommand PickColorCommand { get; }
    public Interaction<ConfirmationWindowViewModel, bool> RequireConfirmation { get; } = new();
    public Interaction<RenameWindowViewModel, string?> InquiryRenameString { get; } = new();
    public Interaction<OpenUriWindowViewModel, string?> InquiryUriString { get; } = new();
    public Interaction<Unit, Uri?> OpenFilePicker { get; } = new();

    public delegate void ErrorStats(Stats errorStats);
    public event ErrorStats ErrorReport = (e) => { };

    public void ChangeImageToPath(string path)
    {
        Path = path;
        ChangeImageToPath();
    }
    public void ChangeImageToPath()
    {
        Path = Path.Trim('"');
        if (string.IsNullOrWhiteSpace(Path)) return;
        //Using index of current dir.
        else if (int.TryParse(Path, out int des) && des >= 1 && des <= Stats.FileCount)
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
    private void OnFSChanged(object sender, FileSystemEventArgs e)
        => fsChanged = true;
    //Set ImageFile and load its directory.
    public void LoadFile(FileInfo file)
    {
        ImageFile = file;
        if (ImageFile.Exists)
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
            fsChanged = true;
            ShowLocalImage();
        }
        else
        {
            Stats = new(false) { File = ImageFile, DisplayName = ImageFile.Name };
            ErrorReport(Stats);
        }
    }
    //Load image files under a directory to CurrentDirItems.
    public void LoadDir(DirectoryInfo dir)
    {
        currentDir = dir;
        CurrentDirItems = dir!.EnumerateFiles("", Recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly)
                              .Where(file => Config.Extensions.Contains(file.Extension.ToLower()))
                              .Where(file => Config.ShowHiddenOrSystemFile || ((file.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0))
                              .OrderBy(file => file.FullName, new IntuitiveStringComparer());
        fsWatcher.Path = currentDir.FullName;
        fsWatcher.IncludeSubdirectories = Recursive;
        fsWatcher.EnableRaisingEvents = true;
    }
    //Set and show info of ImageFile without decoding or rendering.
    public void Select(int offset = 0, int? destination = null)
    {
        if (Stats == null || Stats.File == null || !Stats.File.Exists || Stats.IsWeb || CurrentDirItems == null) return;
        destination ??= Utils.SeekIndex(GetCurrentIndex(), offset, CurrentDirItems.Count());
        var file = CurrentDirItems.ElementAt((int)destination);
        ImageFile = file;
        Path = file.FullName;
        Stats = new(true, offset == 0 && destination == Stats.FileIndex ? Stats : null) { FileIndex = destination, FileCount = CurrentDirItems!.Count(), File = file, DisplayName = file.Name };
    }
    //Return FileInfo of ImageFile or its neighbor.
    public FileInfo? SeekFile(int offset = 0, int? destination = null)
    {
        if (Stats == null || Stats.File == null || !Stats.File.Exists || Stats.IsWeb || CurrentDirItems == null) return null;
        destination ??= Utils.SeekIndex(GetCurrentIndex(), offset, CurrentDirItems.Count());
        return CurrentDirItems.ElementAt((int)destination);
    }
    //Show ImageFile or its neighbor.
    public async void ShowLocalImage(int offset = 0, int? destination = null, bool doPreload = true)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        var files = CurrentDirItems;
        Loading = true;

        if (offset != 0 || Config.Extensions.Contains(ImageFile.Extension.ToLower()))
        {
            if (files == null || !files.Any()) return;
            destination ??= Utils.SeekIndex(GetCurrentIndex(), offset, files.Count());
            var file = files.ElementAt((int)destination);
            ImageFile = file; Path = file.FullName;
            Stats = new(true) { FileIndex = destination, FileCount = files.Count(), File = file, DisplayName = file.Name };

            var oldBitmap = Bitmap;
            using (var fs = file.OpenRead())
                await ShowImage(fs, file.FullName);

            Stats = new(Stats.Success, Stats) { EditCmd = Utils.GetEditAppStartInfo(Stats.File, Stats.Format, Config) };

            if (Config.Preload && doPreload && file.FullName == Path)
                PreloadImage(files, (int)destination);
            else oldBitmap?.Dispose();
        }
        else
        {
            var file = ImageFile; Path = file.FullName;
            Stats = new(true) { FileIndex = null, FileCount = files?.Count(), File = file, DisplayName = file.Name };

            using (var fs = file.OpenRead())
                await ShowImage(fs, file.FullName);

            Stats = new(Stats.Success, Stats) { EditCmd = Utils.GetEditAppStartInfo(Stats.File, Stats.Format, Config) };
        }
        Loading = false;
    }
    //Show image from the stream, use path as identifier.
    public async Task ShowImage(Stream stream, string path)
    {
        var imageInfo = new MagickImageInfo(stream);
        MagickImage? image = null;
        Stats = new(true, Stats) { Format = imageInfo.Format };
        if (imageInfo.Format == MagickFormat.Svg)
        {
            SvgPath = path;
            Bitmap = null;
            Stats = new(true, Stats) { Height = imageInfo.Width, Width = imageInfo.Height };
        }
        else
        {
            Bitmap? bitmapTemp = null;
            if (Config.Preload)
                Preload.TryGetValue(path, out bitmapTemp);
            bitmapTemp ??= await Task.Run(() => { return Utils.ConvertImage(stream, out image); });

            if (path != Path) return;
            Bitmap = bitmapTemp;

            SvgPath = null;
            Stats = (Bitmap == null) ? new(false, Stats)
                                     : new(true, Stats) { Height = Bitmap.Size.Height, Width = Bitmap.Size.Width };
        }
        Magick = image;
        ErrorReport(Stats);
    }
    public async void ShowWebImage(string url)
    {
        Loading = true; ImageFile = null;
        using var webStream = await Utils.GetStreamFromWeb(url);
        if (webStream == null)
            Stats = new(false) { IsWeb = true, Url = url };
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
        if (Stats.FileIndex != null && !fsChanged)
            return (int)Stats.FileIndex;
        fsChanged = false;
        return CurrentDirItems!.IndexOf(ImageFile, Utils.FileInfoComparer);
    }

    //😋 https://chatgpt.com/
    private CancellationTokenSource? preloadCTS = null;
    private readonly Lock preloadLock = new();
    public void PreloadImage(IEnumerable<FileInfo> files, int index)
    {
        if (!files.Any()) return;

        // Cancel previous preloading tasks
        lock (preloadLock)
        {
            preloadCTS?.Cancel();
            preloadCTS = new CancellationTokenSource();
        }

        var token = preloadCTS.Token;
        var preloadTasks = new List<Task>();

        // Define preload range
        int leftRange = Math.Max(-Config.PreloadLeft, -(files.Count() - 1));
        int rightRange = Math.Min(Config.PreloadRight, files.Count() - 1);

        // Identify files to preload
        var newPreloadSet = new HashSet<string>();
        foreach (var offset in Enumerable.Range(leftRange, rightRange - leftRange + 1))
        {
            int preloadIndex = Utils.SeekIndex(index, offset, files.Count());
            var preloadFileName = files.ElementAt(preloadIndex).FullName;
            newPreloadSet.Add(preloadFileName);

            if (offset == 0 && Bitmap != null)
                Preload.TryAdd(preloadFileName, Bitmap);
            else if (token.IsCancellationRequested) break;
            else if (!Preload.ContainsKey(preloadFileName))
            {
                Preload.TryAdd(preloadFileName, null);
                preloadTasks.Add(Task.Run(() => Preload[preloadFileName] = Utils.ConvertImage(files.ElementAt(preloadIndex)), token));
            }
        }

        // Remove preloads outside the range
        var keysToRemove = Preload.Keys.Where(key => !newPreloadSet.Contains(key)).ToList();
        foreach (var key in keysToRemove)
        {
            Preload.Remove(key, out var bm);
            bm?.Dispose();
        }
    }
}
