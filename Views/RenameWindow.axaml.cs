using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using System;
using System.Reactive.Disposables;

namespace ImagePlastic.Views;

public partial class RenameWindow : ReactiveWindow<RenameWindowViewModel>
{
    public RenameWindow()
    {
        InitializeComponent();
        this.WhenActivated(disposables =>
        {
            ViewModel ??= new(new(@"C:\a.png"), false);
            ViewModel.StringInquiry.DenyCommand.Subscribe(Close).DisposeWith(disposables);
            ViewModel.StringInquiry.ConfirmCommand.Subscribe(ViewModel.MovePath ? Move : Rename).DisposeWith(disposables);
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
    private async void Rename(string? newName)
    {
        if (ViewModel!.Config.RenameConfirmation && !await new ConfirmationWindow { DataContext = new ConfirmationWindowViewModel("Rename Confirmation", $"Renaming file {ViewModel!.RenamingFile.FullName} to {newName}") }.ShowDialog<bool>(this)) return;
        try
        {
            FileSystem.RenameFile(ViewModel.RenamingFile.FullName, newName!);
            Close($@"{ViewModel.RenamingFile.DirectoryName}\{newName}");
        }
        catch (Exception e)
        {
            ViewModel!.ErrorMessage = e.Message;
            ErrorMessageTextBlock.IsVisible = true;
        }
    }
    private async void Move(string? newPath)
    {
        if (ViewModel!.Config.MoveConfirmation && !await new ConfirmationWindow { DataContext = new ConfirmationWindowViewModel("Move Confirmation", $"Moving file {ViewModel!.RenamingFile.FullName} to {newPath}") }.ShowDialog<bool>(this)) return;
        try
        {
            FileSystem.MoveFile(ViewModel.RenamingFile.FullName, newPath!);
            Close(newPath);
        }
        catch (Exception e)
        {
            ViewModel!.ErrorMessage = e.Message;
            ErrorMessageTextBlock.IsVisible = true;
        }
    }
    //Make entire window draggable.
    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private bool _mouseDownForWindowMoving = false;
    private PointerPoint _originalPoint;
    private void Window_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving) return;
        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
            Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
    }
    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        if (!point.Properties.IsLeftButtonPressed) return;
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen) return;
        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }
    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _mouseDownForWindowMoving = false;
}