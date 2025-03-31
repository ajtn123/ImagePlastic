using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;

namespace ImagePlastic.Views;

public partial class AboutWindow : ReactiveWindow<AboutWindowViewModel>
{
    public AboutWindow()
    {
        InitializeComponent();
    }
}