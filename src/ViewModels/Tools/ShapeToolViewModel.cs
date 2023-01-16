using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ForzaVinylStudio.Models.Vinyl;

namespace ForzaVinylStudio.ViewModels.Tools;

public class ShapeToolVinylObject
{
    public VinylInfo VinylInfo { get; }
    public BitmapImage ImageSource { get; }

    public ShapeToolVinylObject(VinylInfo vinylInfo)
    {
        VinylInfo = vinylInfo;
        ImageSource = new BitmapImage();
        ImageSource.BeginInit();
        ImageSource.UriSource = new Uri($"Resources/Vinyls/{vinylInfo.Type}/{vinylInfo.TypeIndex}.png", UriKind.Relative);
        ImageSource.CacheOption = BitmapCacheOption.OnLoad; // not sure why but we get IO errors when setting a different cache option
        ImageSource.EndInit();
    }
}

public partial class ShapeToolViewModel : AbstractToolViewModel
{
    public List<ShapeToolVinylObject> Vinyls { get; } = new();
    [ObservableProperty] private ShapeToolVinylObject selectedVinyl;
    [ObservableProperty] private bool popupOpen;

    public ShapeToolViewModel()
    {
        foreach (var type in Enum.GetValues<VinylType>())
        {
            if (type == VinylType.None) continue;
            for (var i = 1; i < 40; i++)
            {
                Vinyls.Add(new ShapeToolVinylObject(new VinylInfo(type, i)));
            }
        }

        selectedVinyl = Vinyls[0];
    }

    [RelayCommand]
    private void OpenPopup()
    {
        PopupOpen = true;
    }

    [RelayCommand]
    private void ClickedVinyl(ShapeToolVinylObject shapeToolVinylObject)
    {
        SelectedVinyl = shapeToolVinylObject;
        PopupOpen = false;
    }
}