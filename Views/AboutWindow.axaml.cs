using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;

namespace ImagePlastic.Views;

public partial class AboutWindow : ReactiveWindow<AboutWindowViewModel>
{
    public AboutWindow()
    {
        InitializeComponent();
        this.WhenActivated(a =>
        {
            ViewModel ??= new();
        });
    }
}