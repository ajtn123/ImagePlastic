using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Reactive;

namespace ImagePlastic.ViewModels;

public class StringInquiryViewModel : ViewModelBase
{
    [Reactive]
    public string? Message { get; set; }
    [Reactive]
    public string Result { get; set; }

    public ReactiveCommand<Unit, string?> ConfirmCommand { get; set; }
    public ReactiveCommand<Unit, string?> DenyCommand { get; set; }

    public StringInquiryViewModel(string defaultResult = "", string? message = null)
    {
        Message = message;
        Result = defaultResult;
        ConfirmCommand = ReactiveCommand.Create<string?>(() => { return Result; });
        DenyCommand = ReactiveCommand.Create<string?>(() => { return null; });
    }
}
