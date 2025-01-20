using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using Microsoft.VisualBasic.FileIO;
using ReactiveUI;
using System;

namespace ImagePlastic.Views;

public partial class RenameWindow : ReactiveWindow<RenameWindowViewModel>
{
    public RenameWindow()
    {
        InitializeComponent();
        this.WhenActivated(a => { Init(); });
    }

    private void Init()
    {
        PointerMoved += PointerMovedHandler;
        PointerPressed += PointerPressedHandler;
        PointerReleased += PointerReleasedHandler;
        ViewModel ??= new(new(@"C:\a.png"));
        ViewModel.StringInquiry.DenyCommand.Subscribe(Close);
        ViewModel.StringInquiry.ConfirmCommand.Subscribe(a =>
        {
            var result = Rename(a);
            if (string.IsNullOrEmpty(result)) return;
            else Close(result);
        });
        StringInquiryView.InquiryBox.Focus();
    }

    private string? Rename(string? newName)
    {
        try
        {
            FileSystem.RenameFile(ViewModel!.RenamingFile.FullName, newName!);
            return $@"{ViewModel.RenamingFile.DirectoryName}\{newName}";
        }
        catch (Exception e)
        {
            ShowError(ViewModel!.ErrorMessage = e.Message);
            return null;
        }
    }
    private void ShowError(string message)
    {
        ViewModel!.ErrorMessage = message;
        ErrorMessageTextBlock.IsVisible = true;
    }

    //Make entire window draggable.
    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private bool _mouseDownForWindowMoving = false;
    private PointerPoint _originalPoint;
    private void PointerMovedHandler(object? sender, PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving) return;
        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
            Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
    }
    private void PointerPressedHandler(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        if (!point.Properties.IsLeftButtonPressed) return;
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen) return;
        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }
    private void PointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
        => _mouseDownForWindowMoving = false;
}