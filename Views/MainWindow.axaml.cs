using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.PanAndZoom;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ImagePlastic.Utilities;
using ImagePlastic.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ImagePlastic.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        KeyDown += KeyDownHandler;
        KeyUp += KeyUpHandler;
        InitializeComponent();
        this.GetObservable(WindowStateProperty).Subscribe(SetWindowStateUI);
        this.WhenActivated(a =>
        {
            DraggableBehavior.SetIsDraggable(TitleBar);
            ProgressDraggableBehavior.SetIsProgressDraggable(Progress);
            Progress.GetPropertyChangedObservable(ProgressDraggableBehavior.ProgressDraggingProperty)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e => ViewModel!.ShowLocalImage(destination: ProgressToRatio((double)e.NewValue!)));
            Progress.GetPropertyChangedObservable(ProgressDraggableBehavior.ProgressDraggingProperty)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(e => ViewModel!.Select(destination: ProgressToRatio((double)e.NewValue!)));
            ViewModel ??= new();
            ViewModel.RequireConfirmation.RegisterHandler(ShowConfirmationWindow);
            ViewModel.InquiryRenameString.RegisterHandler(ShowInquiryWindow);
            ViewModel.InquiryUriString.RegisterHandler(ShowOpenUriWindow);
            ViewModel.OpenFilePicker.RegisterHandler(ShowFilePickerAsync);
            ViewModel.OpenColorPicker.RegisterHandler(ShowColorPickerWindow);
            ViewModel.OpenPropWindow.RegisterHandler(ShowPropWindow);
            ViewModel.OpenAboutWindow.RegisterHandler(ShowAboutWindow);
            ViewModel.CopyToClipboard.RegisterHandler(x => { Clipboard?.SetTextAsync(x.Input); x.SetOutput(Unit.Default); });
            ViewModel.StringInquiryViewModel.ConfirmCommand.Subscribe(s => { ViewModel.ChangeImageToPath(s ?? ""); HidePathBox(); });
            ViewModel.StringInquiryViewModel.DenyCommand.Subscribe(s => HidePathBox());
            this.WhenAnyValue(a => a.ViewModel!.Pinned).Subscribe(b => UpdateTitleBarVisibility(b));
            this.WhenAnyValue(a => a.ViewModel!.Stats.Image).Subscribe(b => RelativePosition.Magick = b);
            this.WhenAnyValue(a => a.ViewModel!.Stats.Bitmap).Subscribe(b => RelativePosition.Bitmap = b);
            this.WhenAnyValue(a => a.ViewModel!.Stats.Success).Subscribe(b => ShowError());
            UpdateTitleBarVisibility(!ViewModel.Config.ExtendImageToTitleBar);
            if (string.IsNullOrEmpty(ViewModel.Path))
            {
                UpdateTitleBarVisibility(true);
                PathBox.IsVisible = true;
                FileName.IsVisible = false;
                PathBox.Focus();
                ZoomText.IsVisible = false;
            }
            RenderOptions.SetBitmapInterpolationMode(BitmapImage, ViewModel.Config.InterpolationMode);
            BitmapImage.PointerMoved += BitmapImage_PointerMoved;
            BitmapImage.PointerPressed += BitmapImage_PointerPressed;
            Zoomer.Focus();
        });
    }

    private void BitmapImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ColorPickerWindow == null) return;

        var isMiddle = e.GetCurrentPoint(sender as Control).Properties.IsMiddleButtonPressed;
        RelativePosition.Frozen = isMiddle;
        UpdateRelativePosition(e);
        ColorPickerWindow.CopyColor();

        if (e.ClickCount >= 2)
            ColorPickerWindow.Close();
    }
    private void BitmapImage_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (RelativePosition.Frozen) return;
        UpdateRelativePosition(e);
    }
    private void UpdateRelativePosition(PointerEventArgs e)
    {
        var pointerPosition = e.GetPosition(BitmapImage);
        RelativePosition.PointerX = pointerPosition.X / BitmapImage.Bounds.Width;
        RelativePosition.PointerY = pointerPosition.Y / BitmapImage.Bounds.Height;
    }

    public List<Window> childWindows = [];
    public List<Window> ChildWindows => childWindows = [.. childWindows.Where(window => window.IsLoaded)];
    public RelativePosition RelativePosition = new();
    public bool ErrorState => ViewModel != null && ViewModel.Stats != null && !ViewModel.Stats.Success;
    public ZoomChangedEventArgs ZoomProperties { get; set; } = new(1, 1, 0, 0);
    public double Scaling => Screens.ScreenFromWindow(this)!.Scaling;
    public int ImageNavigationOffset { get; set; } = 0;

    //Hotkeys
    private void KeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (FocusManager?.GetFocusedElement() is TextBox) return;
        if (e.Key == Key.Left)
            NavigateImage(-1, e.KeyModifiers == KeyModifiers.Control);
        else if (e.Key == Key.Right)
            NavigateImage(1, e.KeyModifiers == KeyModifiers.Control);
        else if (e.Key == Key.OemPlus)
            SetZoom(1);
        else if (e.Key == Key.OemMinus)
            Zoomer.Uniform();
        e.Handled = true;
    }
    private void KeyUpHandler(object? sender, KeyEventArgs e)
    {
        if ((ViewModel?.Stats) != null && !ViewModel.Stats.IsWeb && e.Key is Key.Left or Key.Right)
        {
            if (ImageNavigationOffset >= 2 || ImageNavigationOffset <= -2)
                ViewModel!.ShowLocalImage();
            ImageNavigationOffset = 0;
        }
    }
    //Render every image without skipping if pressing control.
    private void NavigateImage(int offset, bool waitLast = false)
    {
        if (ViewModel?.Stats == null || ViewModel.Stats.IsWeb) return;
        if (ImageNavigationOffset != 0 && !waitLast)
            ViewModel!.Select(offset: offset);
        else if (!waitLast)
            ViewModel!.ShowLocalImage(offset: offset);
        else if (!ViewModel!.Loading || !waitLast)
            ViewModel!.ShowLocalImage(offset: offset, doPreload: false);
        if (ImageNavigationOffset != 0)
            UpdateTitleBarVisibility(true);
        ImageNavigationOffset += offset;
    }

    //Auto resize Title Bar.
    private void Window_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        //if (e.HeightChanged) ;
        if (e.WidthChanged) TitleArea.Width = Width;
    }

    //Auto hide Title Bar.
    private void UpdateTitleBarVisibility(bool visible)
    {
        visible = visible || ErrorState || PathBox.InquiryBox.IsFocused || !ViewModel!.Config.ExtendImageToTitleBar || ViewModel.Pinned;
        WindowControls.IsVisible = visible;
        TitleBar.IsVisible = visible;
    }
    private void StackPanel_PointerEntered(object? sender, PointerEventArgs e)
        => UpdateTitleBarVisibility(true);
    private void StackPanel_PointerExited(object? sender, PointerEventArgs e)
        => UpdateTitleBarVisibility(false);

    //Auto hide left and right Buttons.
    private void Button_PointerEntered(object? sender, PointerEventArgs e)
        => ((Button)sender!).Foreground = Brushes.Black;
    private void Button_PointerExited(object? sender, PointerEventArgs e)
        => ((Button)sender!).Foreground = Brushes.Transparent;

    //Show Error View and make other ui changes.
    private void ShowError()
    {
        var stats = ViewModel!.Stats;
        UpdateTitleBarVisibility(!stats.Success);
        HidePathBox();
        ZoomText.IsVisible = stats.Success;
        Zoomer.IsVisible = stats.Success;
        ErrorView.IsVisible = !stats.Success;
        ErrorView.ErrorMsg.Text = stats.DisplayName != null ? $"Unable to open {stats.DisplayName}" : "Error!";
        if (stats.Bitmap == null) ColorPickerWindow?.Close();
        Zoomer.Uniform();
    }

    //Zoomer and Image scaling.
    private void ResetZoom(object? sender, RoutedEventArgs e)
        => Zoomer.Uniform();
    private void ZoomBorder_ZoomChanged(object sender, ZoomChangedEventArgs e)
        => RefreshZoomDisplay();
    private void ZoomBorder_SizeChanged(object? sender, SizeChangedEventArgs e)
        => RefreshZoomDisplay();
    private void ZoomBorder_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        => ViewModel!.Stretch = StretchMode.None;
    private void ZoomBorder_GotFocus(object? sender, GotFocusEventArgs e)
        => HidePathBox();
    private void Button_Click_1(object? sender, RoutedEventArgs e)
        => SetZoom(1);
    private void Button_Click_2(object? sender, RoutedEventArgs e)
        => Zoomer.Uniform();
    private void RefreshZoomDisplay()
        => ZoomText.Text = $"{double.Round(Zoomer.Bounds.Height * Zoomer.ZoomX * 100 * Scaling / ViewModel!.Stats.Height, 2)}%";
    private void SetZoom(double zoom)
        => Zoomer.Zoom(ViewModel!.Stats.Height * zoom / (Scaling * Zoomer.Bounds.Height), Zoomer.Bounds.Width / 2, Zoomer.Bounds.Height / 2);

    //Shorten path when not on focus.
    private void TextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PathBox.IsVisible = true;
        FileName.IsVisible = false;
        Dispatcher.UIThread.Post(() => PathBox.InquiryBox.Focus(), DispatcherPriority.Background);
    }
    private void HidePathBox()
    {
        if (ViewModel != null && string.IsNullOrWhiteSpace(ViewModel.Path) || ErrorState) return;
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
    private ColorPickerWindow? ColorPickerWindow;
    private void ShowColorPickerWindow(IInteractionContext<ColorPickerWindowViewModel, Unit> context)
    {
        ColorPickerWindow?.Close();
        RelativePosition = context.Input.RelativePosition;
        RelativePosition.Magick = ViewModel?.Stats.Image;
        RelativePosition.Bitmap = ViewModel?.Stats.Bitmap;
        ColorPickerWindow = new() { DataContext = context.Input };
        ChildWindows.Add(ColorPickerWindow);
        ColorPickerWindow.Show(this);
        context.SetOutput(Unit.Default);
    }
    private void ShowPropWindow(IInteractionContext<PropertyWindowViewModel, Unit> context)
    {
        var window = new PropertyWindow() { DataContext = context.Input };
        ChildWindows.Add(window);
        window.Show(this);
        context.SetOutput(Unit.Default);
    }
    private void ShowAboutWindow(IInteractionContext<AboutWindowViewModel, Unit> context)
    {
        var window = new AboutWindow() { DataContext = context.Input };
        ChildWindows.Add(window);
        window.Show(this);
        context.SetOutput(Unit.Default);
    }
    private async Task ShowFilePickerAsync(IInteractionContext<Unit, Uri?> context)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            context.SetOutput(files.Any() ? files[0].Path : null);
        }
        catch (Exception e)
        {
            Trace.WriteLine(e);
            context.SetOutput(null);
        }
    }

    //ProgressBar.
    private void Panel_PointerEntered(object? sender, PointerEventArgs e)
        => Progress.IsVisible = true;
    private void Panel_PointerExited(object? sender, PointerEventArgs e)
        => Progress.IsVisible = false;
    private int ProgressToRatio(double progressRatio)
    {
        if (ViewModel == null || ViewModel.Stats.IsWeb || ViewModel.Stats.FileCount <= 0) return 0;
        var imageIndex = (int)double.Round(progressRatio * ViewModel.Stats.FileCount - 1);
        UpdateTitleBarVisibility(true);
        return Math.Clamp(imageIndex, 0, ViewModel.Stats.FileCount - 1);
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;
        else WindowState = WindowState.Maximized;
    }
    private void FullscreenButton_Click(object? sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.FullScreen)
            WindowState = WindowState.Normal;
        else WindowState = WindowState.FullScreen;
    }
    private void ExitButton_Click(object? sender, RoutedEventArgs e)
        => (ChildWindows.Count != 0 && ViewModel!.Config.OrderedClosing ? ChildWindows.Last() : this).Close();

    private void SetWindowStateUI(WindowState state)
    {
        if (state == WindowState.FullScreen)
        {
            FullscreenIcon.IsVisible = false;
            FullscreenExitIcon.IsVisible = true;
            MaximizeIcon.IsVisible = true;
            MaximizeExitIcon.IsVisible = false;
        }
        else if (state == WindowState.Maximized)
        {
            FullscreenIcon.IsVisible = true;
            FullscreenExitIcon.IsVisible = false;
            MaximizeIcon.IsVisible = false;
            MaximizeExitIcon.IsVisible = true;
        }
        else
        {
            FullscreenIcon.IsVisible = true;
            FullscreenExitIcon.IsVisible = false;
            MaximizeIcon.IsVisible = true;
            MaximizeExitIcon.IsVisible = false;
        }
    }

    private int FullscreenLeftButtonClickCount = 0;
    private int FullscreenRightButtonClickCount = 0;
    private async void FullWindowLeftButtonClick(object? sender, RoutedEventArgs e)
    {
        FullscreenLeftButtonClickCount += 1;
        if (FullWindowLeftArrow.IsVisible)
        {
            FullWindowLeftArrow.IsVisible = false;
            await Task.Delay(100);
        }
        FullWindowLeftArrow.IsVisible = true;
        await Task.Delay(1000);
        if (FullscreenLeftButtonClickCount == 1)
            FullWindowLeftArrow.IsVisible = false;
        FullscreenLeftButtonClickCount -= 1;
    }
    private async void FullWindowRightButtonClick(object? sender, RoutedEventArgs e)
    {
        FullscreenRightButtonClickCount += 1;
        if (FullWindowRightArrow.IsVisible)
        {
            FullWindowRightArrow.IsVisible = false;
            await Task.Delay(100);
        }
        FullWindowRightArrow.IsVisible = true;
        await Task.Delay(1000);
        if (FullscreenRightButtonClickCount == 1)
            FullWindowRightArrow.IsVisible = false;
        FullscreenRightButtonClickCount -= 1;
    }

    public void FileDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetFiles() is { } fileNames && ViewModel != null)
        {
            if (fileNames.First().TryGetLocalPath() is { } fileName)
                ViewModel.ChangeImageToPath(fileName);
        }
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (!ZoomText.IsFocused) return;
        if (double.TryParse(ZoomText.Text, out double v1))
        {
            SetZoom(v1);
            ZoomText.Background = Brushes.Transparent;
        }
        else if (double.TryParse(ZoomText.Text?.TrimEnd('%'), out double v2))
        {
            SetZoom(v2 / 100);
            ZoomText.Background = Brushes.Transparent;
        }
        else ZoomText.Background = Brushes.Red;
    }
}
