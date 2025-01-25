using ImagePlastic.Models;
using ReactiveUI;
using System.IO;

namespace ImagePlastic.ViewModels;

public class RenameWindowViewModel : ViewModelBase
{
    private string? errorMessage;

    public RenameWindowViewModel(FileInfo file, bool movePath, Config config)
    {
        Config = config; MovePath = movePath;
        StringInquiry = MovePath ? new(file.FullName, "Enter a new file path") : new(file.Name, "Enter a new file name");
        RenamingFile = file;
    }
    public StringInquiryViewModel StringInquiry { get; set; }
    public FileInfo RenamingFile { get; set; }
    public string? ErrorMessage { get => errorMessage; set => this.RaiseAndSetIfChanged(ref errorMessage, value); }
    public Config Config { get; set; }
    public bool MovePath { get; set; }
}
