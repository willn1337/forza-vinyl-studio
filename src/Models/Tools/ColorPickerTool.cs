using ForzaVinylStudio.ViewModels.Tools;
using System.Windows.Input;
using System.Windows.Media;

namespace ForzaVinylStudio.Models.Tools;

public class ColorPickerTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("pipette");
    public override Cursor Cursor { get; set; } = GetCursor("pipette");
    public override string Name => "Color Picker";
    public override AbstractToolViewModel ViewModel { get; } = new ColorPickerToolViewModel();
}