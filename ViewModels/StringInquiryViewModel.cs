using ReactiveUI;
using System.Reactive;

namespace ImagePlastic.ViewModels;

public class StringInquiryViewModel : ViewModelBase
{
    public string? Message { get; set; }
    public bool HasMessage { get; set; }
    public string Result { get; set; }

    public ReactiveCommand<Unit, string?> ConfirmCommand { get; set; }
    public ReactiveCommand<Unit, string?> DenyCommand { get; set; }

    public StringInquiryViewModel(string defaultResult = "", string? message = null)
    {
        Message = message;
        HasMessage = message != null;
        Result = defaultResult;
        ConfirmCommand = ReactiveCommand.Create<string?>(() => { return Result; });
        DenyCommand = ReactiveCommand.Create<string?>(() => { return null; });
    }
}
