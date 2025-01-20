using ImagePlastic.Models;

namespace ImagePlastic.ViewModels;

public class OpenUriWindowViewModel : ViewModelBase
{
    public OpenUriWindowViewModel()
    {
        StringInquiry = new(message: "Enter a URI");
        Config ??= new Config();
    }
    public StringInquiryViewModel StringInquiry { get; set; }
    public Config Config { get; set; }
}
