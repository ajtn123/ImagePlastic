using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;

namespace ImagePlastic.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(a => Init());
    }
    public void Init()
    {
        ScalingChanged += ScalingChangedHandler;
        Scaling = Screens.ScreenFromWindow(this)!.Scaling;
        ViewModel!.ErrorReport += ShowError;
        TransparencyLevelHint = ViewModel.Config.Blur;
        //SizeToContent = SizeToContent.WidthAndHeight;
        Application.Current!.TryGetResource("SystemAccentColor", Application.Current.ActualThemeVariant, out object? accentObject);
        var accentColor = accentObject != null ? (Color)accentObject : Color.Parse("#40CFBF");
        accentColor = new Color(127, accentColor.R, accentColor.G, accentColor.B);
        AccentBrush = (IBrush?)ColorToBrushConverter.Convert(accentColor, typeof(IBrush));
        TitleBar.IsVisible = !ViewModel.Config.ExtendImageToTitleBar;
        TitleArea.Background = ViewModel.Config.ExtendImageToTitleBar ? Brushes.Transparent : AccentBrush;
        Grid.SetRow(TitleArea, ViewModel.Config.ExtendImageToTitleBar ? 1 : 0);
    }

    public IBrush? AccentBrush { get; set; }
    public bool ErrorState { get; set; } = false;
    public bool TitleBarPersistent { get; set; } = false;
    public Avalonia.Controls.PanAndZoom.ZoomChangedEventArgs ZoomProperties { get; set; } = new(1, 1, 0, 0);
    public double Scaling { get; set; } = 1;

    /// <summary>
    /// https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    /// </summary>
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
        if (WindowState == Avalonia.Controls.WindowState.Maximized || WindowState == Avalonia.Controls.WindowState.FullScreen) return;
        _mouseDownForWindowMoving = true;
        _originalPoint = e.GetCurrentPoint(this);
    }
    private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _mouseDownForWindowMoving = false;

    private void Window_SizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
    {
        //if (e.HeightChanged) ;
        if (e.WidthChanged) TitleArea.Width = Width;
    }

    private void StackPanel_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!ViewModel!.Config.ExtendImageToTitleBar || ErrorState || TitleBarPersistent) return;
        TitleBar.IsVisible = true;
        TitleArea.Background = AccentBrush;
    }
    private void StackPanel_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (!ViewModel!.Config.ExtendImageToTitleBar || ErrorState || TitleBarPersistent) return;
        TitleBar.IsVisible = false;
        TitleArea.Background = Brushes.Transparent;
    }

    private void Button_PointerEntered(object? sender, Avalonia.Input.PointerEventArgs e)
        => ((Button)sender!).Foreground = AccentBrush;
    private void Button_PointerExited(object? sender, Avalonia.Input.PointerEventArgs e)
        => ((Button)sender!).Foreground = Brushes.Transparent;

    private void ShowError(Stats errorStats)
    {
        ErrorState = !errorStats.Success;
        TitleBar.IsVisible = !errorStats.Success;
        TitleArea.Background = errorStats.Success ? TitleArea.Background = Brushes.Transparent : Brushes.Red;
        Zoomer.IsVisible = errorStats.Success;
        Error.IsVisible = !errorStats.Success;
        if (errorStats.File != null)
            ErrorView.ErrorMsg.Text = $"Unable to open {errorStats.File.FullName}.";
        if (errorStats.Success)
            ResizeImage();
    }
    private void ResetZoom(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Zoomer.ResetMatrix();
    private void ZoomBorder_ZoomChanged(object sender, Avalonia.Controls.PanAndZoom.ZoomChangedEventArgs e)
        => ZoomText.Content = double.Round(e.ZoomX * 100, 2).ToString() + "%";
    private void ScalingChangedHandler(object? sender, EventArgs e)
    {
        Scaling = Screens.ScreenFromWindow(this)!.Scaling;
        ResizeImage();
    }
    private void ResizeImage()
    {
        ImageItself.Height = ViewModel.Bitmap!.Size.Height / Scaling;
        ImageItself.Width = ViewModel.Bitmap!.Size.Width / Scaling;
    }

    private void TextBlock_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        PathBox.IsVisible = true;
        FileName.IsVisible = false;
        PathBox.Focus();
    }
    private void TextBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        TitleBarPersistent = true;
    }
    private void TextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        PathBox.IsVisible = false;
        FileName.IsVisible = true;
        TitleBarPersistent = false;
    }
    //private void ShowKeyDown(object? sender, Avalonia.Input.KeyEventArgs e) => ErrorView.ErrorMsg.Text = e.Key.ToString();
}
