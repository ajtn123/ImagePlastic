using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ImagePlastic.Models;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace ImagePlastic.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(a => Init());
        KeyDown += KeyDownHandler;
        KeyUp += KeyUpHandler;
    }

    public IBrush? AccentBrush { get; set; } = Brushes.Aquamarine;
    public bool ErrorState => ViewModel != null && ViewModel.Stats != null && !ViewModel.Stats.Success;
    public ZoomChangedEventArgs ZoomProperties { get; set; } = new(1, 1, 0, 0);
    public double Scaling => Screens.ScreenFromWindow(this)!.Scaling;
    public int HoldingOffset { get; set; } = 0;

    public void Init()
    {
        if (ViewModel == null) return;
        ViewModel.RequireConfirmation.RegisterHandler(ShowConfirmationWindow);
        ViewModel.InquiryRenameString.RegisterHandler(ShowInquiryWindow);
        ViewModel.InquiryUriString.RegisterHandler(ShowOpenUriWindow);
        ViewModel.OpenFilePicker.RegisterHandler(ShowFilePickerAsync);
        ViewModel.QuitCommand.Subscribe(q => Close());
        ViewModel.ErrorReport += ShowError;
        ViewModel.ChangeImageToPath();
        if (ViewModel.Config.SystemAccentColor)
        {
            Application.Current!.TryGetResource("SystemAccentColor", Application.Current.ActualThemeVariant, out object? accentObject);
            var accentColor = (Color?)accentObject ?? ViewModel.Config.CustomAccentColor;
            accentColor = new Color(127, accentColor.R, accentColor.G, accentColor.B);
            AccentBrush = (IBrush?)ColorToBrushConverter.Convert(accentColor, typeof(IBrush));
        }
        else
            AccentBrush = (IBrush?)ColorToBrushConverter.Convert(ViewModel.Config.CustomAccentColor, typeof(IBrush));
        SwitchBar(!ViewModel.Config.ExtendImageToTitleBar);
        Grid.SetRow(TitleArea, ViewModel.Config.ExtendImageToTitleBar ? 1 : 0);
        if (string.IsNullOrEmpty(ViewModel.Path))
        {
            SwitchBar(true);
            PathBox.IsVisible = true;
            FileName.IsVisible = false;
            PathBox.Focus();
            ZoomText.IsVisible = false;
        }
        RenderOptions.SetBitmapInterpolationMode(BitmapImage, ViewModel.Config.InterpolationMode);
    }

    //Hotkeys
    private void KeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Left && !ViewModel!.Stats.IsWeb)
            if (e.KeyModifiers == KeyModifiers.Control)
                if (!ViewModel!.loading) ViewModel!.ShowLocalImage(offset: -1); else return;
            else
            {
                if (HoldingOffset == 0)
                    ViewModel!.ShowLocalImage(offset: -1);
                else
                {
                    ViewModel!.Select(offset: -1);
                    SwitchBar(true);
                }
                HoldingOffset -= 1;
            }
        else if (e.Key == Key.Right && !ViewModel!.Stats.IsWeb)
            if (e.KeyModifiers == KeyModifiers.Control)
                if (!ViewModel!.loading) ViewModel!.ShowLocalImage(offset: 1); else return;
            else
            {
                if (HoldingOffset == 0)
                    ViewModel!.ShowLocalImage(offset: 1);
                else
                {
                    ViewModel!.Select(offset: 1);
                    SwitchBar(true);
                }
                HoldingOffset += 1;
            }
        //ViewModel!.UIMessage = "KeyDown:" + e.Key.ToString();
    }
    private void KeyUpHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Left or Key.Right && !ViewModel!.Stats.IsWeb)
        {
            if (HoldingOffset >= 2 || HoldingOffset <= -2)
                ViewModel!.ShowLocalImage();
            HoldingOffset = 0;
        }
        //ViewModel!.UIMessage = "KeyUp:" + e.Key.ToString();
    }

    //Make entire window draggable.
    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private bool _mouseDownForWindowMoving = false;
    private PointerPoint _originalPoint;
    private void Window_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_mouseDownForWindowMoving) return;
        PointerPoint currentPoint = e.GetCurrentPoint(this);
        Position = new PixelPoint(Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
            Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y));
    }
    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(sender as Control);
        if (!point.Properties.IsLeftButtonPressed) return;
        if (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen) return;
        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }
    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _mouseDownForWindowMoving = false;

    private void PathBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || string.IsNullOrEmpty(PathBox.Text)) return;
        else ViewModel!.ChangeImageToPath();
    }

    //Auto resize Title Bar.
    private void Window_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        //if (e.HeightChanged) ;
        if (e.WidthChanged) TitleArea.Width = Width;
    }

    //Auto hide Title Bar.
    private void SwitchBar(bool visible)
    {
        visible = visible || (ErrorState || PathBox.IsFocused || !ViewModel!.Config.ExtendImageToTitleBar || ViewModel.Pinned);
        TitleBar.IsVisible = visible;
        TitleArea.Background = ErrorState ? Brushes.Red
                                : visible ? AccentBrush
                                          : Brushes.Transparent;
    }
    private void StackPanel_PointerEntered(object? sender, PointerEventArgs e)
        => SwitchBar(true);
    private void StackPanel_PointerExited(object? sender, PointerEventArgs e)
        => SwitchBar(false);

    //Auto hide left and right Buttons.
    private void Button_PointerEntered(object? sender, PointerEventArgs e)
        => ((Button)sender!).Foreground = AccentBrush;
    private void Button_PointerExited(object? sender, PointerEventArgs e)
        => ((Button)sender!).Foreground = Brushes.Transparent;

    //Show Error View and make other ui changes.
    private void ShowError(Stats errorStats)
    {
        SwitchBar(!errorStats.Success);
        ZoomText.IsVisible = errorStats.Success;
        Zoomer.IsVisible = errorStats.Success;
        ErrorView.IsVisible = !errorStats.Success;
        if (errorStats.File != null)
            ErrorView.ErrorMsg.Text = $"Unable to open {errorStats.File.FullName}";
        else if (errorStats.IsWeb)
            ErrorView.ErrorMsg.Text = $"Unable to open {errorStats.Url}";
        ViewModel!.Stretch = StretchMode.Uniform;
    }

    //Zoomer and Image scaling.
    private void ResetZoom(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel!.Stretch = StretchMode.Uniform;
    private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
        => RefreshZoomDisplay();
    private void ZoomBorder_SizeChanged(object? sender, SizeChangedEventArgs e)
        => RefreshZoomDisplay();
    private void ZoomBorder_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        => ViewModel!.Stretch = StretchMode.None;
    private void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || string.IsNullOrEmpty(ZoomText.Text)) return;
        if (double.TryParse(ZoomText.Text, out double v1))
        {
            SetZoom(v1);
            ZoomText.Background = Brushes.Transparent;
        }
        else if (double.TryParse(ZoomText.Text.TrimEnd('%'), out double v2))
        {
            SetZoom(v2 / 100);
            ZoomText.Background = Brushes.Transparent;
        }
        else ZoomText.Background = Brushes.Red;
    }
    private void Button_Click_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => SetZoom(1);
    private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => ViewModel!.Stretch = StretchMode.Uniform;
    private void RefreshZoomDisplay()
        => ZoomText.Text = $"{double.Round(Zoomer.Bounds.Height * Zoomer.ZoomX * 100 * Scaling / ViewModel!.Stats.Height, 2)}%";
    private void SetZoom(double zoom)
        => Zoomer.Zoom(ViewModel!.Stats.Height * zoom / (Scaling * Zoomer.Bounds.Height), 0, 0);

    //Shorten path when not on focus.
    private void TextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PathBox.IsVisible = true;
        FileName.IsVisible = false;
        PathBox.Focus();
    }
    private void TextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel!.Path) || ErrorState) return;
        PathBox.IsVisible = false;
        FileName.IsVisible = true;
    }

    //Manually hide UIMessage.
    private void TextBlock_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => ViewModel!.UIMessage = null;

    //Show child window.
    private async Task ShowConfirmationWindow(IInteractionContext<ConfirmationWindowViewModel, bool> context)
        => context.SetOutput(await new ConfirmationWindow { DataContext = context.Input }.ShowDialog<bool>(this));
    private async Task ShowInquiryWindow(IInteractionContext<RenameWindowViewModel, string?> context)
        => context.SetOutput(await new RenameWindow { DataContext = context.Input }.ShowDialog<string?>(this));
    private async Task ShowOpenUriWindow(IInteractionContext<OpenUriWindowViewModel, string?> context)
        => context.SetOutput(await new OpenUriWindow { DataContext = context.Input }.ShowDialog<string?>(this));
    private async Task ShowFilePickerAsync(IInteractionContext<Unit, Uri?> context)
    {
        //var topLevel = GetTopLevel(this);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false
        });
        if (files.Any())
            context.SetOutput(files[0].Path);
        else
            context.SetOutput(null);
    }

    //ProgressBar.
    private void Panel_PointerEntered(object? sender, PointerEventArgs e)
        => Progress.IsVisible = true;
    private void Panel_PointerExited(object? sender, PointerEventArgs e)
        => Progress.IsVisible = false;
    private void ProgressBar_PointerEntered(object? sender, PointerEventArgs e)
        => Progress.Height = 10;
    private void ProgressBar_PointerExited(object? sender, PointerEventArgs e)
        => Progress.Height = double.NaN;
    private bool _progressBarPressed = false;
    private void ProgressBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Progress.Height = 12;
        if (ViewModel == null || ViewModel.Stats.IsWeb || ViewModel.Stats.FileCount == null) return;
        SwitchBar(true);
        _progressBarPressed = true;
        var progressRatio = e.GetPosition((Visual?)sender).X / Progress.Bounds.Width;
        var imageIndex = (int)double.Round(progressRatio * (int)ViewModel!.Stats.FileCount - 1);
        if (imageIndex < 0) imageIndex = 0;
        if (imageIndex >= ViewModel!.Stats.FileCount) imageIndex = (int)ViewModel!.Stats.FileCount - 1;
        ViewModel.Select(destination: imageIndex);
    }
    private void ProgressBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_progressBarPressed || ViewModel == null || ViewModel.Stats.IsWeb || ViewModel.Stats.FileCount == null) return;
        var progressRatio = e.GetPosition((Visual?)sender).X / Progress.Bounds.Width;
        var imageIndex = (int)double.Round(progressRatio * (int)ViewModel!.Stats.FileCount - 1);
        if (imageIndex < 0) imageIndex = 0;
        if (imageIndex >= ViewModel!.Stats.FileCount) imageIndex = (int)ViewModel!.Stats.FileCount - 1;
        ViewModel.Select(destination: imageIndex);
    }
    private void ProgressBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        Progress.Height = 10;
        _progressBarPressed = false;
        if (ViewModel == null || ViewModel.Stats.IsWeb || ViewModel.Stats.FileCount == null) return;
        var progressRatio = e.GetPosition((Visual?)sender).X / Progress.Bounds.Width;
        var imageIndex = (int)double.Round(progressRatio * (int)ViewModel!.Stats.FileCount - 1);
        if (imageIndex < 0) imageIndex = 0;
        if (imageIndex >= ViewModel!.Stats.FileCount) imageIndex = (int)ViewModel!.Stats.FileCount - 1;
        ViewModel.ShowLocalImage(destination: imageIndex);
    }
}
