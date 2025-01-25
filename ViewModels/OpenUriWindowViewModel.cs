using ImagePlastic.Models;

namespace ImagePlastic.ViewModels;

public class OpenUriWindowViewModel(Config config) : ViewModelBase
{
    public StringInquiryViewModel StringInquiry { get; set; } = new(message: "Enter a URI");
    public Config Config { get; set; } = config;
}
