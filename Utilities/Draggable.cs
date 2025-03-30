using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ImagePlastic.Utilities;

public static class DraggableBehavior
{
    public static readonly AttachedProperty<bool> IsDraggableProperty
        = AvaloniaProperty.RegisterAttached<Control, bool>("IsDraggable", typeof(DraggableBehavior));
    public static readonly AttachedProperty<bool> IsMouseDownProperty
        = AvaloniaProperty.RegisterAttached<Control, bool>("IsMouseDown", typeof(DraggableBehavior));
    public static readonly AttachedProperty<PointerPoint> OriginalPointProperty
        = AvaloniaProperty.RegisterAttached<Control, PointerPoint>("OriginalPoint", typeof(DraggableBehavior));

    public static bool GetIsDraggable(Control control)
        => control.GetValue(IsDraggableProperty);
    public static void SetIsDraggable(Control control, bool value = true)
    {
        control.SetValue(IsDraggableProperty, value);
        if (value)
        {
            control.PointerMoved += Control_PointerMoved;
            control.PointerPressed += Control_PointerPressed;
            control.PointerReleased += Control_PointerReleased;
            control.PointerCaptureLost += Control_PointerCaptureLost;
            if (control is Window)
                control.LostFocus += Control_LostFocus;
            else if (control.FindAncestorOfType<Window>() is Window window2)
                window2.LostFocus += Control_LostFocus;
        }
        else
        {
            control.PointerMoved -= Control_PointerMoved;
            control.PointerPressed -= Control_PointerPressed;
            control.PointerReleased -= Control_PointerReleased;
            control.PointerCaptureLost -= Control_PointerCaptureLost;
            if (control is Window)
                control.LostFocus -= Control_LostFocus;
            else if (control.FindAncestorOfType<Window>() is Window window2)
                window2.LostFocus -= Control_LostFocus;
        }
    }
    private static void SetIsMouseDown(Control control, bool value)
    => control.SetValue(IsMouseDownProperty, value);
    private static bool GetIsMouseDown(Control control)
        => control.GetValue(IsMouseDownProperty);

    private static void SetOriginalPoint(Control control, PointerPoint value)
        => control.SetValue(OriginalPointProperty, value);
    private static PointerPoint GetOriginalPoint(Control control)
        => control.GetValue(OriginalPointProperty);

    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private static void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control || !GetIsMouseDown(control)) return;
        Window? window = control as Window ?? control.FindAncestorOfType<Window>();
        if (window != null)
        {
            PointerPoint currentPoint = e.GetCurrentPoint(window);
            PointerPoint originalPoint = GetOriginalPoint(control);
            window.Position = new PixelPoint(
                window.Position.X + (int)(currentPoint.Position.X - originalPoint.Position.X),
                window.Position.Y + (int)(currentPoint.Position.Y - originalPoint.Position.Y)
            );
        }
    }
    private static void Control_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        Window? window = control as Window ?? control.FindAncestorOfType<Window>();
        if (window != null && window.WindowState == WindowState.Normal && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
        {
            SetIsMouseDown(control, true);
            SetOriginalPoint(control, e.GetCurrentPoint(window));
        }
    }

    private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
    { if (sender is Control control) SetIsMouseDown(control, false); }
    private static void Control_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    { if (sender is Control control) SetIsMouseDown(control, false); }
    private static void Control_LostFocus(object? sender, RoutedEventArgs e)
    { if (sender is Control control) SetIsMouseDown(control, false); }
}
