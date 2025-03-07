using Avalonia.Controls;
using ImagePlastic.Models;

namespace ImagePlastic.Views;

public partial class StatsView : UserControl
{
    public StatsView()
    {
        InitializeComponent();
        DataContext ??= new Stats(true) { Width = 100, Height = 100, FileCount = 10, FileIndex = 5, IsWeb = false, DisplayName = "001.png", Format = ImageMagick.MagickFormat.Png, File = new(@"C:\Users\chenf\Pictures\kantoku\ [カントク] NOTE 変態王子と笑わない猫。 カントクアートワークス\001.png"), Optimizable = true };
    }
}