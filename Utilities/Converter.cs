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
        if (s == null) return string.Empty;

        List<string> a = [];
        a.Add(string.Empty);
        if (s.FileIndex != null && s.FileCount != null)
            a.Add($"{s.FileIndex + 1}/{s.FileCount}");
        if (s.File != null)
            a.Add(ToReadable(s.File.Length));
        if (s.ImageDimension != null)
            a.Add(s.ImageDimension.ToString()!.Replace(", ", "∗"));
        if (s.File != null)
            a.Add(s.File.LastWriteTime.ToString());
        if (!s.Success)
            a.Add("⊘Filed");
        a.Add(string.Empty);

        return a.Aggregate((x, y) => x + " | " + y);
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
    public static string ToReadable(long length)
    {
        var l = Math.Log2(length);
        if (l >= 0 && l < 10)
            return $"{length} Bytes";
        if (l >= 10 && l < 20)
            return $"{double.Round(length / (double)(1 << 10), 2)} KiB";
        if (l >= 20 && l < 30)
            return $"{double.Round(length / (double)(1 << 20), 2)} MiB";
        if (l >= 30 && l < 40)
            return $"{double.Round(length / (double)(1 << 30), 2)} GiB";
        if (l >= 40)
            return $"{double.Round(length / (double)(1 << 40), 2)} TiB";
        return length.ToString();
    }
}
