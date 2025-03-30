using Avalonia.ReactiveUI;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImagePlastic.Views;

public partial class PropertyWindow : ReactiveWindow<PropertyWindowViewModel>
{
    public PropertyWindow()
    {
        InitializeComponent();
        this.WhenActivated(a =>
        {
            if (Stats == null || ViewModel == null) return;
            DraggableBehavior.SetIsDraggable(this);
            AddMainGroup();
            _ = AddPropGroup(Stats.File);
            _ = AddPropGroup(Stats.Info);
            _ = AddPropGroup(Stats.Stream);
            _ = AddPropGroup(Stats.Image);
        });
    }

    public void AddMainGroup()
    {
        if (ViewModel == null) return;
        List<Prop> mains = [new("File Path", Stats?.File?.FullName ?? "")];
        ViewModel.PropGroups.Add(new("Main", mains) { Expanded = true });
    }
    public static async Task<List<Prop>> IterateProps(object o)
        => await Task.Run(() => o.GetType().GetProperties().Where(prop => prop.CanRead).Select(prop =>
        {
            string value = "";
            try { value = prop.GetValue(o)?.ToString() ?? ""; }
            catch { }
            return new Prop(prop.Name, value);
        }).ToList());
    public async Task AddPropGroup(object? o)
    {
        if (o == null || ViewModel == null) return;
        ViewModel.PropGroups.Add(new(o.GetType().Name, await IterateProps(o)));
    }
    public Stats? Stats => ViewModel?.Stats;
}