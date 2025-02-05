using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ImagePlastic.Views;
using Splat;

namespace ImagePlastic;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        Locator.CurrentMutable.RegisterConstant(ConfigProvider.LoadConfig());
        Locator.CurrentMutable.RegisterConstant(Utils.GetSystemBrush());

        Resources["BoolConverter"] = BoolConverter.Instance;
        Resources["BoolToParameterConverter"] = BoolToParameterConverter.Instance;
        Resources["EnumToBoolConverter"] = EnumToBoolConverter.Instance;
        Resources["IntConverter"] = IntConverter.Instance;
        Resources["StatsConverter"] = StatsConverter.Instance;
        Resources["BlurConverter"] = BlurConverter.Instance;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow { DataContext = new MainWindowViewModel() { Args = desktop.Args } };

        base.OnFrameworkInitializationCompleted();
    }
}