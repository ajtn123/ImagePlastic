using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System;

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
            control.PointerCaptureLost += Control_PointerCaptureLost;
            if (control is Window)
                control.LostFocus += Control_LostFocus;
            else if (control.FindAncestorOfType<Window>() is Window window)
                window.LostFocus += Control_LostFocus;
        }
        else
        {
            control.PointerMoved -= Control_PointerMoved;
            control.PointerPressed -= Control_PointerPressed;
            control.PointerReleased -= Control_PointerReleased;
            control.PointerCaptureLost -= Control_PointerCaptureLost;
            if (control is Window)
                control.LostFocus -= Control_LostFocus;
            else if (control.FindAncestorOfType<Window>() is Window window)
                window.LostFocus -= Control_LostFocus;
        }
    }

    private static bool IsMouseDown;
    private static PointerPoint OriginalPoint;

    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private static void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control || !IsMouseDown) return;
        Window? window = control as Window ?? control.FindAncestorOfType<Window>();
        if (window != null)
        {
            PointerPoint currentPoint = e.GetCurrentPoint(window);
            PointerPoint originalPoint = OriginalPoint;
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
            IsMouseDown = true;
            OriginalPoint = e.GetCurrentPoint(window);
        }
    }

    private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => IsMouseDown = false;
    private static void Control_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        => IsMouseDown = false;
    private static void Control_LostFocus(object? sender, RoutedEventArgs e)
        => IsMouseDown = false;
}

public static class ProgressDraggableBehavior
{
    public static readonly AttachedProperty<bool> IsProgressDraggableProperty
        = AvaloniaProperty.RegisterAttached<ProgressBar, bool>("IsProgressDraggable", typeof(ProgressDraggableBehavior));
    public static readonly AttachedProperty<double> ProgressDraggingProperty
        = AvaloniaProperty.RegisterAttached<ProgressBar, double>("ProgressDragging", typeof(ProgressDraggableBehavior));

    public static bool GetIsProgressDraggable(ProgressBar progressBar)
        => progressBar.GetValue(IsProgressDraggableProperty);
    public static void SetIsProgressDraggable(ProgressBar progressBar, bool value = true)
    {
        progressBar.SetValue(IsProgressDraggableProperty, value);
        if (value)
        {
            progressBar.PointerMoved += ProgressBar_PointerMoved;
            progressBar.PointerPressed += ProgressBar_PointerPressed;
            progressBar.PointerReleased += ProgressBar_PointerReleased;
            progressBar.PointerEntered += ProgressBar_PointerEntered;
            progressBar.PointerExited += ProgressBar_PointerExited;
        }
        else
        {
            progressBar.PointerMoved -= ProgressBar_PointerMoved;
            progressBar.PointerPressed -= ProgressBar_PointerPressed;
            progressBar.PointerReleased -= ProgressBar_PointerReleased;
            progressBar.PointerEntered -= ProgressBar_PointerEntered;
            progressBar.PointerExited -= ProgressBar_PointerExited;
        }
    }

    private static bool ProgressBarPressed;
    private static void ProgressBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ProgressBar progressBar) return;
        progressBar.Height = 12;
        ProgressBarPressed = true;
        progressBar.SetValue(ProgressDraggingProperty, e.GetPosition((Visual?)sender).X / progressBar.Bounds.Width);
    }
    private static void ProgressBar_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not ProgressBar progressBar) return;
        if (ProgressBarPressed)
            progressBar.SetValue(ProgressDraggingProperty, e.GetPosition((Visual?)sender).X / progressBar.Bounds.Width);
    }
    private static void ProgressBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not ProgressBar progressBar) return;
        progressBar.Height = 10;
        ProgressBarPressed = false;
        progressBar.SetValue(ProgressDraggingProperty, e.GetPosition((Visual?)sender).X / progressBar.Bounds.Width);
    }

    private static void ProgressBar_PointerEntered(object? sender, PointerEventArgs e)
        => ((ProgressBar)sender!).Height = 10;
    private static void ProgressBar_PointerExited(object? sender, PointerEventArgs e)
        => ((ProgressBar)sender!).Height = double.NaN;
}

public class ProximityVisibilityBehavior
{
    public static readonly AttachedProperty<bool> IsProximalVisible
        = AvaloniaProperty.RegisterAttached<Control, bool>("IsProximalVisible", typeof(ProximityVisibilityBehavior));
    public static readonly AttachedProperty<double> VisibleRadius
        = AvaloniaProperty.RegisterAttached<Control, double>("VisibleRadius", typeof(ProximityVisibilityBehavior));

    public static bool GetProximityVisibility(Control control)
        => control.GetValue(IsProximalVisible);
    public static void SetProximityVisibility(Control control, bool value = true, double radius = 50)
    {
        control.SetValue(IsProximalVisible, value);
        control.SetValue(VisibleRadius, radius);
        if (value) control.PointerMoved += Control_PointerMoved;
        else control.PointerMoved -= Control_PointerMoved;
    }

    private static void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Control control) return;

        var pointerPos = e.GetPosition(control);
        var bounds = control.Bounds;

        var center = new Point(bounds.Width / 2, bounds.Height / 2);
        var distance = Math.Sqrt(Math.Pow(pointerPos.X - center.X, 2) + Math.Pow(pointerPos.Y - center.Y, 2));

        control.IsVisible = distance < control.GetValue(VisibleRadius);
    }
}
