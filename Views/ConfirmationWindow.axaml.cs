using Avalonia.ReactiveUI;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace ImagePlastic.Views;

public partial class ConfirmationWindow : ReactiveWindow<ConfirmationWindowViewModel>
{
    public ConfirmationWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            DraggableBehavior.SetIsDraggable(this);
            ViewModel ??= new("Notice", "Msg");
            ViewModel.ConfirmCommand.Subscribe(result => Close(result)).DisposeWith(disposables);
            ViewModel.DenyCommand.Subscribe(result => Close(result)).DisposeWith(disposables);
        });
    }
}