using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ImagePlastic.Utilities;

public static class DraggableBehavior
{
    public static readonly AttachedProperty<bool> IsDraggableProperty
        = AvaloniaProperty.RegisterAttached<Control, bool>("IsDraggable", typeof(DraggableBehavior));
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
        }
        else
        {
            control.PointerMoved -= Control_PointerMoved;
            control.PointerPressed -= Control_PointerPressed;
            control.PointerReleased -= Control_PointerReleased;
        }
    }

    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private static bool _mouseDownForWindowMoving = false;
    private static PointerPoint _originalPoint;
    private static void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_mouseDownForWindowMoving && sender is Window window)
        {
            PointerPoint currentPoint = e.GetCurrentPoint(window);
            window.Position = new PixelPoint(
                window.Position.X + (int)(currentPoint.Position.X - _originalPoint.Position.X),
                window.Position.Y + (int)(currentPoint.Position.Y - _originalPoint.Position.Y)
            );
        }
    }
    private static void Control_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Window window && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed && window.WindowState == WindowState.Normal)
        {
            _mouseDownForWindowMoving = true;
            _originalPoint = e.GetCurrentPoint(window);
        }
    }
    private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _mouseDownForWindowMoving = false;
}
