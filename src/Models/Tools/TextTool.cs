using ForzaVinylStudio.ViewModels.Tools;
using System.Windows.Media;

namespace ForzaVinylStudio.Models.Tools;

public class TextTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("layer-shape-text");
    public override string Name => "Text";
    public override AbstractToolViewModel? ViewModel => null;
}