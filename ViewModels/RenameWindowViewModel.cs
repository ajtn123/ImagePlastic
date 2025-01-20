using ImagePlastic.Models;
using ReactiveUI;
using System.IO;

namespace ImagePlastic.ViewModels;

public class RenameWindowViewModel : ViewModelBase
{
    private string? errorMessage;

    public RenameWindowViewModel(FileInfo file)
    {
        StringInquiry = new(file.Name, "Enter a new file name");
        RenamingFile = file;
        Config ??= new Config();
    }
    public StringInquiryViewModel StringInquiry { get; set; }
    public FileInfo RenamingFile { get; set; }
    public string? ErrorMessage { get => errorMessage; set => this.RaiseAndSetIfChanged(ref errorMessage, value); }
    public Config Config { get; set; }
}
