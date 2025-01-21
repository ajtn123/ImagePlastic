using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;
namespace ImagePlastic.Views;

public partial class StringInquiry : ReactiveUserControl<StringInquiryViewModel>
{
    public StringInquiry()
    {
        InitializeComponent();
        this.WhenActivated(a => { ViewModel ??= new("", "Enter Some Words"); });
    }
}