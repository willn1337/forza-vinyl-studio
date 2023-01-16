using System.Windows.Input;
using System.Windows.Media;
using ForzaVinylStudio.Models.Vinyl;
using ForzaVinylStudio.ViewModels.Tools;
using SkiaSharp.Views.WPF;

namespace ForzaVinylStudio.Models.Tools;

public class ShapeTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("layer-shape");
    public override string Name => "Shape";
    public override ShapeToolViewModel ViewModel { get; } = new();

    public override void OnMouseDown(MouseButtonEventArgs e)
    {
        if(e.ChangedButton != MouseButton.Left) return;

        foreach (var shape in VinylGroup.Shapes)
        {
            shape.Selected = false;
        }

        var vinyl = new VinylShape(VinylGroup, VinylRenderData.Get(ViewModel.SelectedVinyl.VinylInfo))
        {
            X = Canvas.MappedMousePos.X,
            Y = Canvas.MappedMousePos.Y,
            Color = App.MainWindow.ViewModel.SelectedColor.ToSKColor(),
            Selected = true,
        };
        vinyl.InvalidateMappedVertices();
        vinyl.InvalidateMappedColors();
        vinyl.InvalidatePreview();

        VinylGroup.Shapes.Insert(VinylGroup.SelectedShape == null 
            ? 0 : VinylGroup.Shapes.IndexOf(VinylGroup.SelectedShape),
            vinyl);
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        Canvas.TryInvalidateVisual();
    }
}