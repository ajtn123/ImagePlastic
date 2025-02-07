using Avalonia.Media;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace ImagePlastic.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public ViewModelBase()
    {
        AccentBrush = Config.SystemAccentColor ? Utils.GetSystemBrush(Config.SystemAccentColorOpacity) ?? Brush.Parse(Config.CustomAccentColor) : Brush.Parse(Config.CustomAccentColor);
    }
    public Config Config { get; set; } = Locator.Current.GetService<Config>()!;
    [Reactive]
    public IBrush? AccentBrush { get; set; }
}
