using System.Windows.Controls.Ribbon;
using System.Windows.Media;

namespace ForzaVinylStudio.Views.Controls;

public class FlatIconButton : RibbonButton
{
    public FlatIconButton()
    {
        MouseOverBackground = Brushes.LightGray;
        ContextMenu = null;
    }
}