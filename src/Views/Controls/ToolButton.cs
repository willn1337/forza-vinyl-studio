using System.Linq;
using System.Windows.Controls.Ribbon;
using System.Windows.Media;
using ForzaVinylStudio.Models.Tools;

namespace ForzaVinylStudio.Views.Controls;

public class ToolButton : RibbonToggleButton
{
    public ToolButton()
    {
        MouseOverBackground = Brushes.LightGray;
        CheckedBackground = Brushes.CornflowerBlue;
        ContextMenu = null;
    }

    protected override void OnToggle()
    {
        if (IsChecked == true)
        {
            return;
        }

        base.OnToggle();

        App.MainWindow.ViewModel.SetCurrentTool(DataContext.GetType());
    }
}