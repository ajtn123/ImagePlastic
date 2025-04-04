using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ImagePlastic.UserControls;
using System;
using System.Collections.Generic;

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
            if (control.FindAncestorOfType<Window>(true) is Window window)
                window.LostFocus += Control_LostFocus;
        }
        else
        {
            control.PointerMoved -= Control_PointerMoved;
            control.PointerPressed -= Control_PointerPressed;
            control.PointerReleased -= Control_PointerReleased;
            control.PointerCaptureLost -= Control_PointerCaptureLost;
            if (control.FindAncestorOfType<Window>(true) is Window window)
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
    public static readonly AttachedProperty<List<Control>> ProximalVisibleControlsProperty
        = AvaloniaProperty.RegisterAttached<Control, List<Control>>("ProximalVisibleControls", typeof(ProximityVisibilityBehavior));
    public static readonly AttachedProperty<bool> IsProximalVisibleProperty
        = AvaloniaProperty.RegisterAttached<Control, bool>("IsProximalVisible", typeof(ProximityVisibilityBehavior));
    public static readonly AttachedProperty<double> VisibleRadiusProperty
        = AvaloniaProperty.RegisterAttached<Control, double>("VisibleRadius", typeof(ProximityVisibilityBehavior));

    public static bool GetProximityVisibility(Control control)
        => control.GetValue(IsProximalVisibleProperty);
    public static void SetProximityVisibility(Control control, bool value = true, double radius = 50)
    {
        if (value)
        {
            if (control.FindAncestorOfType<Window>() is Window window)
            {
                var controls = window.GetValue(ProximalVisibleControlsProperty) ?? [];
                window.SetValue(ProximalVisibleControlsProperty, controls);

                controls.Add(control);

                if (controls.Count == 1)
                    window.PointerMoved += Control_PointerMoved;
            }
            else return;
        }
        else
        {
            if (control.FindAncestorOfType<Window>() is Window window)
            {
                var controls = window.GetValue(ProximalVisibleControlsProperty);

                controls?.Remove(control);

                if (controls != null && controls.Count == 0)
                    window.PointerMoved -= Control_PointerMoved;
            }
            else return;
        }

        control.SetValue(IsProximalVisibleProperty, value);
        control.SetValue(VisibleRadiusProperty, radius);
    }

    private static void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Window window) return;

        foreach (var control in window.GetValue(ProximalVisibleControlsProperty))
        {
            var pointerPos = e.GetPosition(control);
            var bounds = control.Bounds;

            var x = pointerPos.X < 0 ? -pointerPos.X
                : pointerPos.X > bounds.Width ? pointerPos.X - bounds.Width
                : 0;
            var y = pointerPos.Y < 0 ? -pointerPos.Y
                : pointerPos.Y > bounds.Height ? pointerPos.Y - bounds.Height
                : 0;

            var distance = Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2));

            if (control is TitleArea titleArea)
                titleArea.Visibility = distance <= control.GetValue(VisibleRadiusProperty);

            else control.Opacity = distance <= control.GetValue(VisibleRadiusProperty) ? 1 : 0;
        }
    }
}
