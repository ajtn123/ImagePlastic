using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace ImagePlastic.Views;

public partial class ConfirmationWindow : ReactiveWindow<ConfirmationWindowViewModel>
{
    public ConfirmationWindow()
    {
        InitializeComponent();
        ViewModel ??= new("Notice", "Msg");
        this.WhenActivated(a => ViewModel!.ConfirmCommand.Subscribe(a => Close(a)));
        this.WhenActivated(a => ViewModel!.DenyCommand.Subscribe(a => Close(a)));
    }
}