using System.Windows.Input;
using System.Windows.Media;
using ForzaVinylStudio.ViewModels.Tools;
using SkiaSharp.Views.WPF;

namespace ForzaVinylStudio.Models.Tools;

public class PaintBucketTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("paint-can");
    public override Cursor Cursor { get; set; } = GetCursor("paint-can-flipped");
    public override string Name => "Paint Bucket";
    public override AbstractToolViewModel? ViewModel => null;

    public override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            TrySetColor();
        }
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            TrySetColor();
        }
        Canvas.TryInvalidateVisual();
    }

    private static void TrySetColor()
    {
        var shape = SelectTool.HitTest();
        if (shape != null)
        {
            shape.Color = App.MainWindow.ViewModel.SelectedColor.ToSKColor();
        }
    }
}