using ReactiveUI;
using System.Reactive;

namespace ImagePlastic.ViewModels;

public class StringInquiryWindowViewModel
{
    public string Title { get; set; }
    public string? Message { get; set; }
    public bool HasMessage { get; set; }
    public string Result { get; set; }

    public ReactiveCommand<Unit, string> ConfirmCommand { get; set; }
    public ReactiveCommand<Unit, string> DenyCommand { get; set; }

    public StringInquiryWindowViewModel(string title, string defaultResult = "", string? message = null)
    {
        Title = title;
        Message = message;
        HasMessage = message != null;
        Result = defaultResult;
        ConfirmCommand = ReactiveCommand.Create(() => { return Result; });
        DenyCommand = ReactiveCommand.Create(() => { return string.Empty; });
    }
}
