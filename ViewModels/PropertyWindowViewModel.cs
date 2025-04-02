using ImagePlastic.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public class PropertyWindowViewModel : ViewModelBase
{
    public Stats? Stats { get; set; }
    public ObservableCollection<PropGroup> PropGroups { get; set; } = [];
}

public class Prop(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}
public class PropGroup(string groupName, List<Prop> props)
{
    public string GroupName { get; set; } = groupName;
    public string? CommandName { get; set; }
    public ICommand? Command { get; set; }
    public bool Expanded { get; set; } = false;
    public List<Prop> Props { get; set; } = props;
}