﻿using Avalonia.Data.Converters;
using ImagePlastic.Models;
using ImagePlastic.Utilities;
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
        if (s.File != null && s.File.Exists)
            a.Add(Utils.ToReadable(s.File.Length));
        if (s.ImageDimension != null)
            a.Add(s.ImageDimension.ToString()!.Replace(", ", "∗"));
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
