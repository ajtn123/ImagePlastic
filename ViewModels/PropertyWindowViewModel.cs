using ImagePlastic.Models;
using System.Collections.ObjectModel;

namespace ImagePlastic.ViewModels;

public class PropertyWindowViewModel : ViewModelBase
{
    public Stats? Stats { get; set; }
    public ObservableCollection<Prop> Props { get; set; } = [];
}

public class Prop(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}