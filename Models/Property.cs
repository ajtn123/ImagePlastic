using System.Collections.Generic;
using System.Windows.Input;

namespace ImagePlastic.Models;

public class Prop(string name, string value)
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;
}
public class PropGroup(string groupName, List<Prop> props)
{
    public string GroupName { get; set; } = groupName;
    public string? CommandName { get; set; }
    public ICommand? Command { get; set; }
    public int NameColumnWidth { get; set; } = 200;
    public bool Expanded { get; set; } = false;
    public List<Prop> Props { get; set; } = props;
}