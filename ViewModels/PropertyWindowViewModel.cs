using ImagePlastic.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public class PropertyWindowViewModel : ViewModelBase
{
    public required Stats Stats { get; set; }
    public ObservableCollection<PropGroup> PropGroups { get; set; } = [];
    public ICommand? ShowInExplorerCommand { get; set; }
    public ICommand? ShowExplorerPropCommand { get; set; }
    public ICommand? SaveCommand { get; set; }
    public ICommand? EditCommand { get; set; }
}