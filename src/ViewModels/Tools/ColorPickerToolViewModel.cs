using CommunityToolkit.Mvvm.ComponentModel;

namespace ForzaVinylStudio.ViewModels.Tools;

public partial class ColorPickerToolViewModel : AbstractToolViewModel
{
    [ObservableProperty] private bool pickColorFromShape;
}