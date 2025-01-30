using ImagePlastic.Models;
using ReactiveUI;
using System.Reactive;

namespace ImagePlastic.ViewModels;

public class ConfirmationWindowViewModel : ViewModelBase
{
    public string Title { get; set; }
    public string Message { get; set; }

    public ReactiveCommand<Unit, bool> ConfirmCommand { get; set; }
    public ReactiveCommand<Unit, bool> DenyCommand { get; set; }

    public ConfirmationWindowViewModel(string title, string message)
    {
        Title = title;
        Message = message;
        ConfirmCommand = ReactiveCommand.Create(() => true);
        DenyCommand = ReactiveCommand.Create(() => false);
    }
}
