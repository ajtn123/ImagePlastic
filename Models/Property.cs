using Avalonia.Media;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Collections.Generic;
using System.Windows.Input;

namespace ImagePlastic.Models;

public class Prop(string name, string value) : ReactiveObject
{
    [Reactive]
    public string Name { get; set; } = name;
    [Reactive]
    public string Value { get; set; } = value;
    [Reactive]
    public double NameWidth { get; set; } = 200;
}

public class PropGroup() : ReactiveObject
{
    [Reactive]
    public required string GroupName { get; set; }
    [Reactive]
    public required List<Prop> Props { get; set; }
    [Reactive]
    public string? CommandName { get; set; }
    [Reactive]
    public ICommand? Command { get; set; }
    [Reactive]
    public bool Expanded { get; set; } = false;
    [Reactive]
    public IBrush? AccentBrush { get; set; } = Brush.Parse("#7FFFFFFF");
}