using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public class PropertyWindowViewModel : ViewModelBase
{
    public PropertyWindowViewModel()
    {
        OpenExplorerCommand = ReactiveCommand.Create(() => { if (Stats?.File != null) Utils.SelectInExplorer(Stats.File); });
        OpenExplorerPropCommand = ReactiveCommand.Create(() => { if (Stats?.File != null) ExplorerPropertiesOpener.OpenFileProperties(Stats.File.FullName); });
        EditCommand = ReactiveCommand.Create(() => { if (Stats?.EditCmd != null) Process.Start(Stats.EditCmd); });
    }
    public required Stats Stats { get; set; }
    public ObservableCollection<PropGroup> PropGroups { get; set; } = [];
    public ICommand OpenExplorerCommand { get; }
    public ICommand OpenExplorerPropCommand { get; }
    public ICommand EditCommand { get; }
}