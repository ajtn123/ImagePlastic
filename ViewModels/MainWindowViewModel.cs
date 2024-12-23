using Avalonia.Media.Imaging;
using DynamicData;
using ImagePlastic.Models;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel(string[]? args = null)
    {
        Args = args;
        config = new Config();
        if (Config.DefaultFile != null)
            path = Config.DefaultFile.FullName;
        if (args != null && args.Length > 0)
            path = args[0];
        RefreshImage();
        GoLeft = ReactiveCommand.Create(() => { RefreshImage(-1); });
        GoRight = ReactiveCommand.Create(() => { RefreshImage(1); });
    }

    private Config config;
    private Bitmap? bitmap;
    private FileInfo? imageFile;
    private string path = "";
    private string status = "";
    private int fileIndex;

    public string[]? Args { get; }
    public Config Config { get => config; set => this.RaiseAndSetIfChanged(ref config, value); }
    public Bitmap? Bitmap { get => bitmap; set => this.RaiseAndSetIfChanged(ref bitmap, value); }
    public FileInfo? ImageFile { get => imageFile; set => this.RaiseAndSetIfChanged(ref imageFile, value); }
    public string Path { get => path; set => this.RaiseAndSetIfChanged(ref path, value); }
    public int FileIndex { get => fileIndex; set => this.RaiseAndSetIfChanged(ref fileIndex, value); }
    public string Status { get => status; set => this.RaiseAndSetIfChanged(ref status, value); }

    public ICommand GoLeft { get; }
    public ICommand GoRight { get; }

    public void RefreshImage(int offset = 0)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            ImageFile = new FileInfo(Path);
            if (!ImageFile.Exists) return;
            var files = ImageFile.Directory!.EnumerateFiles().Where(a => config.Extensions.Contains(a.Extension.ToLower())).Select(a => a.FullName.ToLower());
            var currentIndex = files.IndexOf(ImageFile.FullName.ToLower());
            var destination = (currentIndex + offset) >= files.Count() ? (currentIndex + offset) - files.Count() : (currentIndex + offset) < 0 ? (currentIndex + offset) + files.Count() : (currentIndex + offset);
            Path = files.ElementAt(destination);
            FileIndex = destination;

            ImageFile = new FileInfo(Path);
            Bitmap = new Bitmap(ImageFile.FullName);
            Status = $" | {FileIndex + 1}/{files.Count()} | {ImageFile.Length} | {Bitmap.Size.ToString().Replace(", ", "*")} | {ImageFile.LastWriteTime}";
        }
        catch
        {
            Status = "❌";
        }
    }
}
