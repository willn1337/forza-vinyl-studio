using System.Windows.Input;
using System.Windows.Media;
using ForzaVinylStudio.ViewModels.Tools;

namespace ForzaVinylStudio.Models.Tools;

public class ZoomTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("magnifier-left");
    public override Cursor Cursor { get; set; } = GetCursor("magnifier-left");
    public override string Name => "Zoom";
    public override AbstractToolViewModel? ViewModel => null;

    public override void OnMouseUp(MouseButtonEventArgs e)
    {
        const float zoomStep = .8f;

        float scale;
        if (e.ChangedButton == MouseButton.Left)
        {
            scale = 1f / zoomStep;
        }
        else if (e.ChangedButton == MouseButton.Right)
        {
            scale = zoomStep;
        }
        else return;

        Canvas.Zoom(scale);
    }

    
}