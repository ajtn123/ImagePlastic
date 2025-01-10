using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using ImagePlastic.Models;
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

    public IBrush? AccentBrush { get; set; }
    public bool ErrorState { get; set; } = false;
    public bool TitleBarPersistent { get; set; } = false;
    public ZoomChangedEventArgs ZoomProperties { get; set; } = new(1, 1, 0, 0);
    public double Scaling { get; set; } = 1;
    public int HoldingOffset { get; set; } = 0;

    public void Init()
    {
        ScalingChanged += ScalingChangedHandler;
        Scaling = Screens.ScreenFromWindow(this)!.Scaling;
        ViewModel!.ErrorReport += ShowError;
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
        KeyDown += KeyDownHandler;
        KeyUp += KeyUpHandler;
    }

    //Hotkeys
    private void KeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Left && !ViewModel!.Stats.IsWeb)
            if (e.KeyModifiers == KeyModifiers.Control)
                if (!ViewModel!.loading) ViewModel!.RefreshImage(offset: -1); else return;
            else
            {
                if (HoldingOffset == 0)
                    ViewModel!.RefreshImage(offset: -1);
                else
                {
                    ViewModel!.Select(-1);
                    SwitchBar(true);
                }
                HoldingOffset -= 1;
            }
        else if (e.Key == Key.Right && !ViewModel!.Stats.IsWeb)
            if (e.KeyModifiers == KeyModifiers.Control)
                if (!ViewModel!.loading) ViewModel!.RefreshImage(offset: 1); else return;
            else
            {
                if (HoldingOffset == 0)
                    ViewModel!.RefreshImage(offset: 1);
                else
                {
                    ViewModel!.Select(1);
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
                ViewModel!.RefreshImage();
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
        visible = (!ViewModel!.Config.ExtendImageToTitleBar || ErrorState || TitleBarPersistent) || visible;
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
        ErrorState = !errorStats.Success;
        SwitchBar(!errorStats.Success);
        Zoomer.IsVisible = errorStats.Success;
        Error.IsVisible = !errorStats.Success;
        ZoomText.IsVisible = errorStats.Success;
        if (errorStats.File != null)
            ErrorView.ErrorMsg.Text = $"Unable to open {errorStats.File.FullName}";
        Zoomer.Stretch = StretchMode.Uniform;
    }

    //Zoomer and Image scaling.
    private void ResetZoom(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Zoomer.Stretch = StretchMode.Uniform;
    private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
        => RefreshZoomDisplay();
    private void ZoomBorder_SizeChanged(object? sender, SizeChangedEventArgs e)
        => RefreshZoomDisplay();
    private void ZoomBorder_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        => Zoomer.Stretch = StretchMode.None;
    private void ScalingChangedHandler(object? sender, EventArgs e)
        => Scaling = Screens.ScreenFromWindow(this)!.Scaling;
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
    {
        if (ImageItself.Source != null) SetZoom(1);
    }
    private void Button_Click_2(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        => Zoomer.Stretch = StretchMode.Uniform;
    private void RefreshZoomDisplay()
        => ZoomText.Text = ImageItself.Source != null ? $"{double.Round(Zoomer.Bounds.Height * Zoomer.ZoomX * 100 * Scaling / ImageItself.Source.Size.Height, 2)}%" : null;
    private void SetZoom(double zoom)
        => Zoomer.Zoom(ImageItself.Source!.Size.Height * zoom / (Scaling * Zoomer.Bounds.Height), 0, 0);

    //Shorten path when not on focus.
    private void TextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PathBox.IsVisible = true;
        FileName.IsVisible = false;
        PathBox.Focus();
    }
    private void TextBox_GotFocus(object? sender, GotFocusEventArgs e)
        => TitleBarPersistent = true;
    private void TextBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(ViewModel!.Path)) return;
        PathBox.IsVisible = false;
        FileName.IsVisible = true;
        TitleBarPersistent = false;
    }

    private void TextBlock_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => ViewModel!.UIMessage = null;
}
