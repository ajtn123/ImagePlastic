using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;

namespace ImagePlastic.Views;

public partial class OpenUriWindow : ReactiveWindow<OpenUriWindowViewModel>
{
    public OpenUriWindow()
    {
        InitializeComponent();
        this.WhenActivated(a => { Init(); });
    }
    private void Init()
    {
        ViewModel ??= new();
        ViewModel.StringInquiry.DenyCommand.Subscribe(Close);
        ViewModel.StringInquiry.ConfirmCommand.Subscribe(Close);
        StringInquiryView.InquiryBox.Focus();
    }
}