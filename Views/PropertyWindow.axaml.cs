using Avalonia.ReactiveUI;
using ImageMagick;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImagePlastic.Views;

public partial class PropertyWindow : ReactiveWindow<PropertyWindowViewModel>
{
    public PropertyWindow()
    {
        InitializeComponent();
        this.WhenActivated(a =>
        {
            if (ViewModel == null) return;
            DraggableBehavior.SetIsDraggable(this);
            AddMainGroup();
            _ = AddPropGroup(Stats.File);
            _ = AddPropGroup(Stats.Info);
            _ = AddPropGroup(Stats.Stream);
            _ = AddPropGroup(Stats.Image);
            _ = AddSvgTextGroup();
            if (Stats.File?.FullName is string filePath)
                ViewModel.PropGroups.Add(new() { GroupName = "Shell", Props = ShellPropertyHelper.IterateFileProperties(filePath), Command = ReactiveCommand.Create(() => ViewModel.PropGroups.Insert(2, new() { GroupName = "Shell Property Map", Props = ShellPropertyHelper.GetMap() })), CommandName = "Property Map" });
        });
    }

    private Stats Stats => ViewModel!.Stats;

    private void AddMainGroup()
    {
        if (ViewModel == null) return;
        List<Prop> mains = [
            new("File Name", Stats.File?.Name ?? ""),
            new("File Path", Stats.File?.FullName ?? "")
            ];
        ViewModel.PropGroups.Add(new() { GroupName = "Main", Props = mains, Expanded = true });
    }

    private async Task AddSvgTextGroup()
    {
        if (ViewModel == null || Stats.Info?.Format != MagickFormat.Svg || Stats.Stream == null) return;
        Stats.Stream.Position = 0;
        TextReader tr = new StreamReader(Stats.Stream);
        var text = await tr.ReadToEndAsync();
        List<Prop> textGroup = [new("", text) { NameWidth = 0 }];
        ViewModel.PropGroups.Add(new() { GroupName = "SVG markup", Props = textGroup, Command = ReactiveCommand.Create(() => { Clipboard?.SetTextAsync(text); }), CommandName = "Copy" });
    }

    public async Task AddPropGroup(object? o)
    {
        if (o == null || ViewModel == null) return;
        ViewModel.PropGroups.Add(new() { GroupName = o.GetType().Name, Props = await Utils.IteratePropsToPropList(o) });
    }

    private void Close(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Close();
}