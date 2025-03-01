using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace ImagePlastic.Views;

public partial class OpenUriWindow : ReactiveWindow<OpenUriWindowViewModel>
{
    public OpenUriWindow()
    {
        InitializeComponent();
        Deactivated += (a, b) => Close();
        this.WhenActivated(disposables =>
        {
            DraggableBehavior.SetIsDraggable(this);
            ViewModel ??= new();
            ViewModel.StringInquiry.ConfirmCommand.Subscribe(Close).DisposeWith(disposables);
            ViewModel.StringInquiry.DenyCommand.Subscribe(Close).DisposeWith(disposables);
            StringInquiryView.InquiryBox.Focus();
        });
    }
    private void Window_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var x = (e.PreviousSize.Width - e.NewSize.Width) / 2 * Scaling;
        var y = (e.PreviousSize.Height - e.NewSize.Height) / 2 * Scaling;
        Position = new PixelPoint(Position.X + (int)x, Position.Y + (int)y);
    }
    private double Scaling => Screens.ScreenFromWindow(this)!.Scaling;
}