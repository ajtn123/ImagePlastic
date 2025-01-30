namespace ImagePlastic.ViewModels;

public class OpenUriWindowViewModel : ViewModelBase
{
    public StringInquiryViewModel StringInquiry { get; set; } = new(message: "Enter a URI");
}
