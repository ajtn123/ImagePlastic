﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

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
                control.LostFocus += Control_LostFocus;
            else if (control.FindAncestorOfType<Window>() is Window window2)
                window2.LostFocus += Control_LostFocus;
        }
    }

    //https://github.com/AvaloniaUI/Avalonia/discussions/8441#discussioncomment-3081536
    private static bool _mouseDownForWindowMoving = false;
    private static PointerPoint _originalPoint;
    private static void Control_PointerMoved(object? sender, PointerEventArgs e)
    {
        Window? window = null;
        if (_mouseDownForWindowMoving && sender is Control control)
            if (control is Window window1) window = window1;
            else if (control.FindAncestorOfType<Window>() is Window window2) window = window2;
            else return;
        if (window != null)
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
        Window? window = null;
        if (sender is Control control && e.GetCurrentPoint(window).Properties.IsLeftButtonPressed)
            if (control is Window window1) window = window1;
            else if (control.FindAncestorOfType<Window>() is Window window2) window = window2;
            else return;
        if (window != null && window.WindowState == WindowState.Normal)
        {
            _mouseDownForWindowMoving = true;
            _originalPoint = e.GetCurrentPoint(window);
        }
    }
    private static void Control_PointerReleased(object? sender, PointerReleasedEventArgs e)
        => _mouseDownForWindowMoving = false;
    private static void Control_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    => _mouseDownForWindowMoving = false;
    private static void Control_LostFocus(object? sender, RoutedEventArgs e)
        => _mouseDownForWindowMoving = false;
}
