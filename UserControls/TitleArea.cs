using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ImagePlastic.Views;

namespace ImagePlastic.UserControls;

public class TitleArea : Panel
{
    public static readonly StyledProperty<bool> VisibilityProperty
        = AvaloniaProperty.Register<TitleArea, bool>(nameof(Visibility), true, coerce: CoerceVisibility);

    public bool Visibility
    {
        get => GetValue(VisibilityProperty);
        set => SetValue(VisibilityProperty, value);
    }

    private static bool CoerceVisibility(AvaloniaObject sender, bool value)
    {
        if (sender is not TitleArea titleArea) return value;

        value = titleArea.FindAncestorOfType<MainWindow>()?.UpdateTitleBarVisibility(value) ?? true;

        titleArea.Opacity = value ? 1 : 0;
        return value;
    }
}
