using System.Windows.Input;
using System.Windows.Media;
using ForzaVinylStudio.ViewModels.Tools;
using SkiaSharp;

namespace ForzaVinylStudio.Models.Tools;

public class PanTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("hand");
    public override Cursor Cursor { get; set; } = Cursors.Hand;
    public override string Name => "Pan";
    public override AbstractToolViewModel? ViewModel => null;

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (!Canvas.MouseActuallyDown) return;

        VinylGroup.ViewMatrix = SKMatrix.Concat(
            SKMatrix.CreateTranslation(Canvas.MousePos.X - Canvas.MouseDragPos.X,
                Canvas.MousePos.Y - Canvas.MouseDragPos.Y),
            VinylGroup.ViewMatrix);

        Canvas.TryInvalidateVisual();
    }

}