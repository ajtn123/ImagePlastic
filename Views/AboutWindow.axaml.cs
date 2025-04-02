using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;

namespace ImagePlastic.Views;

public partial class AboutWindow : ReactiveWindow<AboutWindowViewModel>
{
    public AboutWindow()
    {
        InitializeComponent();
        this.WhenActivated(a =>
        {
            ViewModel ??= new();
            this.WhenAnyValue(a => a.ViewModel!.HasUpdate).Subscribe(u => UpdateButton.Classes.Set("Pro", u));
        });
    }
}