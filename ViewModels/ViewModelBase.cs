using ImagePlastic.Models;
using ReactiveUI;
using Splat;

namespace ImagePlastic.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public Config Config { get; set; } = Locator.Current.GetService<Config>()!;
}
