using Avalonia.Controls.PanAndZoom;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
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
        Stats = new();
        if (Config.DefaultFile != null)
            Path = Config.DefaultFile.FullName;
        fsWatcher = new();
        //FSWatcher.Changed += OnChanged;
        fsWatcher.Created += OnFSChanged;
        fsWatcher.Deleted += OnFSChanged;
        fsWatcher.Renamed += OnFSChanged;
        Stretch = Config.Stretch;
        Recursive = Config.RecursiveSearch;
        this.WhenAnyValue(vm => vm.Stats).Subscribe(stats => stats.PropertyChanged += UpdateStats);
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
            UpdateStats();
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
                FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, Config.MoveToRecycleBin ? RecycleOption.SendToRecycleBin : RecycleOption.DeletePermanently);

                ImageFile = fallbackFile;
                ShowLocalImage();
                UIMessage = $"{file.FullName} is {(Config.MoveToRecycleBin ? "sent to recycle bin" : "deleted permanently")}.";
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                UIMessage = e.Message;
            }
        });
        EditCommand = ReactiveCommand.Create(() =>
        {
            if (Stats.EditCmd != null)
                Process.Start(Stats.EditCmd);
        });
        ShowInExplorerCommand = ReactiveCommand.Create(() =>
        {
            if (Stats.File != null)
                Utils.SelectInExplorer(Stats.File);
        });
        RenameCommand = ReactiveCommand.Create(async () =>
        {
            var file = Stats.File;
            if (Stats.IsWeb == true || file == null) return;

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
        SaveCommand = ReactiveCommand.Create(async () =>
        {
            if (Stats.Stream == null || !Stats.IsWeb) return;
            using var file = await OpenSaveFilePicker.Handle(Stats.DisplayName ?? "");
            Stats.Stream.CopyTo(await file.OpenWriteAsync());
        });
        OpenLocalCommand = ReactiveCommand.Create(async () =>
        {
            var files = await OpenFilePicker.Handle(new());
            recursiveDir = null;
            if (files != null && files.Count >= 1 && files[0].TryGetLocalPath() is string path)
                LoadFile(new(path));
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
        PickColorCommand = ReactiveCommand.Create(async () =>
        {
            if (Stats.Image == null) return;
            _ = await OpenColorPicker.Handle(new());
        });
        RotateCommand = ReactiveCommand.Create(() =>
        {
            if (Stats.Image == null) return;
            Stats.Image.Rotate(90);
            _ = ShowMagickImageAsync(Stats.Image);
        });
        CopyPathCommand = ReactiveCommand.Create(async () =>
        {
            var path = Stats.File?.FullName ?? Stats.Url;
            if (path == null) return;
            if (Config.PathCopyQuotation == PathQuotation.Always || (Config.PathCopyQuotation == PathQuotation.ContainSpace && path.Contains(' ')))
                path = path.Insert(0, "\"") + "\"";
            _ = await CopyToClipboard.Handle(path);
        });
        OpenPropCommand = ReactiveCommand.Create(async () =>
        {
            var vm = new PropertyWindowViewModel() { Stats = Stats, ShowInExplorerCommand = ShowInExplorerCommand, ShowExplorerPropCommand = ShowExplorerPropCommand, EditCommand = EditCommand, SaveCommand = SaveCommand };
            _ = await OpenPropWindow.Handle(vm);
        });
        ShowExplorerPropCommand = ReactiveCommand.Create(() =>
        {
            if (Stats.File != null)
                ExplorerPropertiesOpener.OpenFileProperties(Stats.File.FullName);
        });
        OpenAboutCommand = ReactiveCommand.Create(async () =>
        {
            var vm = new AboutWindowViewModel();
            _ = await OpenAboutWindow.Handle(vm);
        });
    }

    private void UpdateStats()
        => this.RaisePropertyChanged(nameof(Stats));
    private void UpdateStats(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => UpdateStats();

    private string[]? args;
    private DirectoryInfo? currentDir;
    private DirectoryInfo? recursiveDir = null;
    private bool loading = false;
    private bool loadingIndication = false;
    public bool fsChanged = false;
    private readonly FileSystemWatcher fsWatcher;
    private bool recursive;
    public Dictionary<string, Stats?> Preload = [];
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
    public string Path { get => StringInquiryViewModel.Result; set => StringInquiryViewModel.Result = value; }
    public StringInquiryViewModel StringInquiryViewModel { get; set; } = new(message: "Image Path");
    public IOrderedEnumerable<Stats>? Pics { get; set; }
    [Reactive]
    public Stats Stats { get; set; }
    [Reactive]
    public StretchMode Stretch { get; set; }
    public bool Loading { get => loading; set { this.RaiseAndSetIfChanged(ref loading, value); this.RaiseAndSetIfChanged(ref loadingIndication, Config.LoadingIndicator && value, nameof(LoadingIndication)); } }
    public bool LoadingIndication => loadingIndication;
    [Reactive]
    public string? UIMessage { get; set; }
    [Reactive]
    public bool Pinned { get; set; }
    [Reactive]
    public bool WindowOnTop { get; set; }
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
    public ICommand SaveCommand { get; }
    public ICommand OpenLocalCommand { get; }
    public ICommand OpenUriCommand { get; }
    public ICommand ReloadDirCommand { get; }
    public ICommand ConfigureCommand { get; }
    public ICommand PickColorCommand { get; }
    public ICommand RotateCommand { get; }
    public ICommand CopyPathCommand { get; }
    public ICommand OpenPropCommand { get; }
    public ICommand ShowExplorerPropCommand { get; }
    public ICommand OpenAboutCommand { get; }
    public Interaction<ConfirmationWindowViewModel, bool> RequireConfirmation { get; } = new();
    public Interaction<RenameWindowViewModel, string?> InquiryRenameString { get; } = new();
    public Interaction<OpenUriWindowViewModel, string?> InquiryUriString { get; } = new();
    public Interaction<ColorPickerWindowViewModel, Unit> OpenColorPicker { get; } = new();
    public Interaction<PropertyWindowViewModel, Unit> OpenPropWindow { get; } = new();
    public Interaction<AboutWindowViewModel, Unit> OpenAboutWindow { get; } = new();
    public Interaction<string, Unit> CopyToClipboard { get; } = new();
    public Interaction<Unit, IReadOnlyList<IStorageFile>?> OpenFilePicker { get; } = new();
    public Interaction<string, IStorageFile?> OpenSaveFilePicker { get; } = new();

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
            Stats = new() { Success = false };
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
            Stats = new() { Success = false, File = ImageFile, DisplayName = ImageFile.Name };
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
        Pics = CurrentDirItems.Select(file => new Stats() { File = file }).OrderBy(s => s.File!.FullName, new IntuitiveStringComparer());
        fsWatcher.Path = currentDir.FullName;
        fsWatcher.IncludeSubdirectories = Recursive;
        fsWatcher.EnableRaisingEvents = true;
    }
    //Set and show info of ImageFile without decoding or rendering.
    public void Select(int offset = 0, int destination = -1)
    {
        if (Stats == null || Stats.File == null || !Stats.File.Exists || Stats.IsWeb || CurrentDirItems == null) return;
        if (destination == -1) destination = Utils.SeekIndex(GetCurrentIndex(), offset, CurrentDirItems.Count());
        var file = CurrentDirItems.ElementAt(destination);
        ImageFile = file;
        Path = file.FullName;
        if (offset == 0 && destination == Stats.FileIndex)
        {
            Stats.FileIndex = destination;
            Stats.FileCount = CurrentDirItems!.Count();
            Stats.File = file;
            Stats.DisplayName = file.Name;
        }
        else Stats = new() { FileIndex = destination, FileCount = CurrentDirItems!.Count(), File = file, DisplayName = file.Name, Info = new(file) };
    }
    //Return FileInfo of ImageFile or its neighbor.
    public FileInfo? SeekFile(int offset = 0, int destination = -1)
    {
        if (Stats == null || Stats.File == null || !Stats.File.Exists || Stats.IsWeb || CurrentDirItems == null) return null;
        if (destination == -1) destination = Utils.SeekIndex(GetCurrentIndex(), offset, CurrentDirItems.Count());
        return CurrentDirItems.ElementAt(destination);
    }
    //Show ImageFile or its neighbor.
    public async void ShowLocalImage(int offset = 0, int destination = -1, bool doPreload = true)
    {
        if (ImageFile == null || !ImageFile.Exists) return;
        var files = CurrentDirItems;
        Loading = true;

        if (offset != 0 || Config.Extensions.Contains(ImageFile.Extension.ToLower()))
        {
            if (files == null || !files.Any()) return;
            if (destination == -1) destination = Utils.SeekIndex(GetCurrentIndex(), offset, files.Count());
            var file = files.ElementAt(destination);
            ImageFile = file; Path = file.FullName;
            Stats = new() { FileCount = files.Count(), FileIndex = destination, File = file, DisplayName = file.Name };

            var oldBitmap = Stats.Bitmap;
            using (var fs = file.OpenRead())
                await ShowImage(fs, file.FullName);

            Stats.EditCmd = Utils.GetEditAppStartInfo(Stats.File, Stats.Format, Config);

            if (Config.Preload && doPreload && file.FullName == Path)
                PreloadImage(files, destination);
            else oldBitmap?.Dispose();
        }
        else
        {
            var file = ImageFile; Path = file.FullName;
            Stats = new() { File = file, DisplayName = file.Name };

            using (var fs = file.OpenRead())
                await ShowImage(fs, file.FullName);

            Stats.EditCmd = Utils.GetEditAppStartInfo(Stats.File, Stats.Format, Config);
        }
        Loading = false;
    }
    //Show image from the stream, use path as identifier.
    public async Task ShowImage(Stream stream, string path)
    {
        var imageInfo = new MagickImageInfo(stream);
        MagickImage? image = null;
        Stats.Info = imageInfo;
        Stats.Stream = stream.CloneStream();
        if (imageInfo.Format == MagickFormat.Svg)
        {
            Stats.SvgPath = path;
            Stats.Bitmap = null;
        }
        else
        {
            Bitmap? bitmapTemp = null;
            if (Config.Preload)
            { Preload.TryGetValue(path, out var s); bitmapTemp = s?.Bitmap; image = s?.Image; }
            bitmapTemp ??= await Task.Run(() => { return Utils.ConvertImage(stream, out image); });

            if (path != Path) return;
            Stats.Bitmap = bitmapTemp;

            Stats.SvgPath = null;
            if (Stats.Bitmap == null)
                Stats.Success = false;
        }
        Stats.Image = image;
        ErrorReport(Stats);
    }
    public async Task ShowMagickImageAsync(MagickImage magick)
    {
        Stats.Bitmap = await Task.Run(() => { return Utils.ConvertImage(magick); });
        Stats.SvgPath = null;
        if (Stats.Bitmap == null)
            Stats.Success = false;
    }
    public async void ShowWebImage(string url)
    {
        Loading = true; ImageFile = null;
        using var webStream = await Utils.GetStreamFromWeb(url);
        Stats = new() { Success = webStream != null, IsWeb = true, Url = url, DisplayName = url.Split('/')[^1] };

        if (webStream != null) await ShowImage(webStream, url);

        ErrorReport(Stats);
        Loading = false;
    }
    //Get index of current file.
    public int GetCurrentIndex()
    {
        if (Stats.FileIndex >= 0 && !fsChanged)
            return Stats.FileIndex;
        fsChanged = false;
        return CurrentDirItems!.IndexOf(ImageFile, Utils.FileInfoComparer);
    }

    //😋 https://chatgpt.com/
    private CancellationTokenSource? preloadCTS = null;
    private readonly Lock preloadLock = new();
    private void PreloadImage(IEnumerable<FileInfo> files, int index)
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

            if (offset == 0 && Stats.Bitmap != null)
                Preload.TryAdd(preloadFileName, Stats);
            else if (token.IsCancellationRequested) break;
            else if (!Preload.ContainsKey(preloadFileName))
            {
                Preload.TryAdd(preloadFileName, null);
                preloadTasks.Add(Task.Run(() => Preload[preloadFileName] = new() { Bitmap = Utils.ConvertImage(files.ElementAt(preloadIndex), out var image), Image = image }, token));
            }
        }

        // Remove preloads outside the range
        var keysToRemove = Preload.Keys.Where(key => !newPreloadSet.Contains(key)).ToList();
        foreach (var key in keysToRemove)
        {
            Preload.Remove(key, out var s);
            s?.Dispose();
        }
    }
}
