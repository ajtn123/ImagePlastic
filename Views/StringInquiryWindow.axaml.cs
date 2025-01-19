using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Linq;
namespace ImagePlastic.Views;

public partial class StringInquiryWindow : ReactiveWindow<StringInquiryWindowViewModel>
{
    public StringInquiryWindow()
    {
        InitializeComponent();
        ViewModel ??= new("Enter Some Words");
        this.WhenActivated(a => ViewModel!.ConfirmCommand.Subscribe(a => Close(a)));
        this.WhenActivated(a => ViewModel!.DenyCommand.Subscribe(a => Close(a)));
    }
}