using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public class PropertyWindowViewModel : ViewModelBase
{
    public PropertyWindowViewModel()
    {
        OpenExplorerCommand = ReactiveCommand.Create(() => { if (Stats?.File != null) Utils.SelectInExplorer(Stats.File); });
        EditCommand = ReactiveCommand.Create(() => { if (Stats != null && Stats.EditCmd != null) Process.Start(Stats.EditCmd); });
    }
    public Stats? Stats { get; set; }
    public ObservableCollection<PropGroup> PropGroups { get; set; } = [];
    public ICommand OpenExplorerCommand { get; }
    public ICommand EditCommand { get; }
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
    public int NameColumnWidth { get; set; } = 200;
    public bool Expanded { get; set; } = false;
    public List<Prop> Props { get; set; } = props;
}