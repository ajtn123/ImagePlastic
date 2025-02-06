using Avalonia.Media;
using ImagePlastic.Models;
using ReactiveUI;
using Splat;

namespace ImagePlastic.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public ViewModelBase()
    {
        AccentBrush = Config.SystemAccentColor ? Locator.Current.GetService<IBrush>() ?? Brush.Parse(Config.CustomAccentColor) : Brush.Parse(Config.CustomAccentColor);
    }
    public Config Config { get; set; } = Locator.Current.GetService<Config>()!;
    public IBrush? AccentBrush { get; set; }
}
