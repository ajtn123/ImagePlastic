using ImagePlastic.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Windows.Input;

namespace ImagePlastic.ViewModels;

public class AboutWindowViewModel : ViewModelBase
{
    public AboutWindowViewModel()
    {
        AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        OpenRepoCommand = ReactiveCommand.Create(() => Utils.OpenUrl("https://github.com/ajtn123/ImagePlastic"));
        OpenReleaseCommand = ReactiveCommand.Create(() => Utils.OpenUrl("https://github.com/ajtn123/ImagePlastic/releases"));
        OpenProfileCommand = ReactiveCommand.Create(() => Utils.OpenUrl("https://github.com/ajtn123"));
        CheckUpdateCommand = ReactiveCommand.Create(async () =>
        {
            var newestVersion = await UpdateChecker.GetGitHubVersion();
            HasUpdate = UpdateChecker.CheckUpdate(newestVersion);
            UpdateMessage = $"{(HasUpdate ? "Update available" : "No update available")}: {newestVersion}";
        });
    }

    [Reactive]
    public string? AppName { get; set; }
    [Reactive]
    public string? AppVersion { get; set; }
    [Reactive]
    public bool HasUpdate { get; set; } = false;
    [Reactive]
    public string? UpdateMessage { get; set; }

    public ICommand OpenRepoCommand { get; }
    public ICommand OpenReleaseCommand { get; }
    public ICommand OpenProfileCommand { get; }
    public ICommand CheckUpdateCommand { get; }
}
