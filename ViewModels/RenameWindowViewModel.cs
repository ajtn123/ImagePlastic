using ReactiveUI.Fody.Helpers;
using System.IO;

namespace ImagePlastic.ViewModels;

public class RenameWindowViewModel : ViewModelBase
{
    public RenameWindowViewModel(FileInfo file, bool movePath)
    {
        MovePath = movePath;
        StringInquiry = MovePath ? new(file.FullName, "Enter a new file path") : new(file.Name, "Enter a new file name");
        RenamingFile = file;
    }
    public StringInquiryViewModel StringInquiry { get; set; }
    public FileInfo RenamingFile { get; set; }
    [Reactive]
    public string? ErrorMessage { get; set; }
    public bool MovePath { get; set; }
}
