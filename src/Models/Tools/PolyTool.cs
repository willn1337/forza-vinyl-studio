using ForzaVinylStudio.ViewModels.Tools;
using System.Windows.Media;

namespace ForzaVinylStudio.Models.Tools;

public class PolyTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("layer-shape-polygon");
    public override string Name => "Polygon";
    public override AbstractToolViewModel? ViewModel => null;
}