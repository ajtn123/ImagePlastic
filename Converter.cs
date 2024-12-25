using Avalonia.Data.Converters;
using ImagePlastic.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ImagePlastic.Converter;

public class StatsConverter : IValueConverter
{
    public static readonly StatsConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = (Stats)value!;
        if (s == null) return "";
        if (!s.Success) return " | ❌Filed";
        List<string> a = [];
        a.Add("");
        if (s.FileIndex != null && s.FileCount != null)
            a.Add($"{s.FileIndex + 1}/{s.FileCount}");
        if (s.File != null)
            a.Add(s.File.Length.ToString());
        if (s.ImageDimension != null)
            a.Add(s.ImageDimension.ToString()!.Replace(", ", "∗"));
        if (s.File != null)
            a.Add(s.File.LastWriteTime.ToString());
        return a.Aggregate((x, y) => x + " | " + y);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
