using Avalonia.Input;
using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
namespace ImagePlastic.Views;

public partial class StringInquiry : ReactiveUserControl<StringInquiryViewModel>
{
    public StringInquiry()
    {
        InitializeComponent();
        ViewModel ??= new("", "Enter Some Words");
    }
}