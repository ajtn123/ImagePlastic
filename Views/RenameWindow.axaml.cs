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
}