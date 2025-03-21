using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using ShimSkiaSharp;

namespace ImagePlastic.Views;

public partial class PropertyWindow : ReactiveWindow<PropertyWindowViewModel>
{
    public PropertyWindow()
    {
        InitializeComponent();
        this.WhenActivated(a =>
        {
            if (Stats == null) return;
            if (Stats.File != null)
            {
                Add("Path", Stats.File.FullName);
                Add("Last Write Time", Stats.File.LastWriteTime.ToString());
                Add("Last Access Time", Stats.File.LastAccessTime.ToString());
                Add("Creation Time", Stats.File.CreationTime.ToString());
                Add("File Size", Utils.ToReadable(Stats.File.Length));
            }
            if (Stats.Url != null)
            {
                Add("URL", Stats.Url.ToString());
            }
        });
    }

    public void Add(string name, string value)
    {
        ViewModel?.Props.Add(new(name, value));
    }
    public Stats? Stats => ViewModel?.Stats;
}