﻿using Avalonia;
using Avalonia.ReactiveUI;
using System;

namespace ImagePlastic;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
        => BuildAvaloniaApp()
          .StartWithClassicDesktopLifetime(args);
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .WithInterFont()
                     .With(new SkiaOptions { MaxGpuResourceSizeBytes = 1 << 30 })
                     .LogToTrace()
                     .UseReactiveUI();
}
