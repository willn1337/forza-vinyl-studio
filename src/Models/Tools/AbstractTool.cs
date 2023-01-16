using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ForzaVinylStudio.Models.Vinyl;
using ForzaVinylStudio.ViewModels.Tools;
using ForzaVinylStudio.Views.Controls;
using SkiaSharp;

namespace ForzaVinylStudio.Models.Tools;

public abstract partial class AbstractTool : ObservableObject
{
    protected static VinylGroup VinylGroup => App.MainWindow.ViewModel.CurrentVinylGroup;
    protected static VinylCanvasControl Canvas => App.MainWindow.VinylCanvasControl;

    [ObservableProperty] private bool selected;
    private Cursor cursor = Cursors.Arrow;

    public virtual Cursor Cursor
    {
        get => cursor;
        set
        {
            if (cursor != value)
            {
                Canvas.Cursor = value;
                cursor = value;
            }
        }
    }

    public abstract ImageSource Icon { get; }
    public abstract string Name { get; }
    public abstract AbstractToolViewModel? ViewModel { get; }

    public virtual void OnMouseDown(MouseButtonEventArgs e) { }

    public virtual void OnMouseMove(MouseEventArgs e) { }

    public virtual void OnMouseUp(MouseButtonEventArgs e) { }

    public virtual void OnMouseWheel(MouseWheelEventArgs e) { }

    protected static ImageSource GetIcon(string name)
    {
        return new BitmapImage(new Uri($"../Resources/Icons/{name}.png", UriKind.Relative));
    }

    protected static Cursor GetCursor(string name)
    {
        var resourceStream = Application.GetResourceStream(new Uri($"Resources/Cursors/{name}.cur", UriKind.Relative));
        if (resourceStream == null) throw new Exception("Couldn't load cursor: " + name);
        return new Cursor(resourceStream.Stream);
    }
}