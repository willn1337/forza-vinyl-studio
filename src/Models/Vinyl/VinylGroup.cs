using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SkiaSharp;

namespace ForzaVinylStudio.Models.Vinyl;

public partial class VinylGroup : ObservableObject
{
    public const string DefaultName = "Untitled";

    public ObservableCollection<VinylShape> Shapes { get; } = new();
    [ObservableProperty] private string name;
    [ObservableProperty] private SKMatrix viewMatrix = SKMatrix.Identity;
    [ObservableProperty] private SKColor backgroundColor = new (194, 194, 186);
    [ObservableProperty] private VinylShape? selectedShape;

    public VinylGroup(string name = DefaultName)
    {
        this.name = name;
    }
}