using Avalonia.Controls;
using Avalonia.Data.Converters;
using ImagePlastic.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ImagePlastic.Utilities;

public class StatsConverter : IValueConverter
{
    public static readonly StatsConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (Stats?)value;
        if (s == null) return string.Empty;

        List<string> a = [];
        a.Add(string.Empty);
        //Show the actual image format if it is different from the file extension.
        if (s.DisplayName != null && s.Format != default && !s.DisplayName.Split('.')[^1].Equals(s.Format.ToString(), StringComparison.OrdinalIgnoreCase))
            a.Add(s.Format.ToString());
        if (s.FileIndex != null || s.FileCount != null)
            a.Add($"{(s.FileIndex ?? -1) + 1}/{s.FileCount ?? 0}");
        if (s.File != null && s.File.Exists)
            a.Add(Utils.ToReadable(s.File.Length));
        if (!double.IsNaN(s.Height) && !double.IsNaN(s.Width))
            a.Add($"{s.Height}*{s.Width}");
        if (s.File != null && s.File.Exists)
            a.Add(s.File.LastWriteTime.ToString());
        if (!s.Success)
            a.Add("⊘Filed");
        a.Add(string.Empty);

        return a.Aggregate((x, y) => x + " | " + y);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolConverter : IValueConverter
{
    public static readonly BoolConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value != null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BoolToParameterConverter : IValueConverter
{
    public static readonly BoolToParameterConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (bool?)value == true ? parameter : null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class IntConverter : IValueConverter
{
    public static readonly IntConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!int.TryParse((string?)parameter, out int result) || (int?)value == null) return null;
        else return (int)value + result;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class EnumToBoolConverter : IValueConverter
{
    public static readonly EnumToBoolConverter Instance = new();
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value?.ToString() == parameter?.ToString();
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class BlurConverter : IValueConverter
{
    public static readonly BlurConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ((Transparency[]?)value)?.Select(item => item switch
        {
            Transparency.None => WindowTransparencyLevel.None,
            Transparency.Transparent => WindowTransparencyLevel.Transparent,
            Transparency.Blur => WindowTransparencyLevel.Blur,
            Transparency.AcrylicBlur => WindowTransparencyLevel.AcrylicBlur,
            Transparency.Mica => WindowTransparencyLevel.Mica,
            _ => WindowTransparencyLevel.None,
        }).ToList();
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}