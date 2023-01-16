using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using ForzaVinylStudio.Models.Tools;
using ForzaVinylStudio.ViewModels;
using ForzaVinylStudio.ViewModels.Tools;
using SkiaSharp;
using SkiaSharp.Views.WPF;
using Xceed.Wpf.Toolkit;

namespace ForzaVinylStudio.Views;

public partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        DataContext = ViewModel;
    }
    
    private void ColorPicker_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.ColorSwatches.Contains(ViewModel.SelectedColor)) return;

        ViewModel.ColorSwatches.Add(ViewModel.SelectedColor);
        if (ViewModel.ColorSwatches.Count >= 16)
        {
            ViewModel.ColorSwatches.RemoveAt(0);
        }
    }

    private void ColorSwatch_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ColorSwatches.Remove((Color)((Rectangle)sender).DataContext);
    }

    private void ColorSwatch_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var color = (Color)((Rectangle)sender).DataContext;
        ColorPicker.SelectedColor = color;
        var colorItem = new ColorItem(color, color.ToString());
        if (!ColorPicker.RecentColors.Contains(colorItem))
        {
            ColorPicker.RecentColors.Add(colorItem);
        }

        // xceed color picker seems to limit to 10 recent colors at once
        if (ColorPicker.RecentColors.Count >= 10)
        {
            ColorPicker.RecentColors.RemoveAt(0);
        }

        UpdateSelectedShapesColors();
    }

    private void OnShapePropertyChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        // this event gets fired (ValueChanged) when we modify the shape via select tool transformations, causing a lot of lag.
        if (((Control)sender).IsMouseOver)
        {
            ViewModel.CurrentVinylGroup.SelectedShape?.InvalidatePreview();
        }
    }

    private void ShapeList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (((Control)sender).IsMouseOver)
        {
            ViewModel.CurrentVinylGroup.SelectedShape =
                ViewModel.CurrentVinylGroup.Shapes.FirstOrDefault(shape => shape.Selected);
            if (ViewModel.CurrentTool is SelectTool tool)
            {
                tool.InvalidateShapesTransformRect();
            }
            VinylCanvasControl.TryInvalidateVisual();
        }
    }

    private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
    {
        if (((Control)sender).IsMouseOver)
        {
            UpdateSelectedShapesColors();
        }
    }

    private void UpdateSelectedShapesColors()
    {
        foreach (var shape in ViewModel.CurrentVinylGroup.Shapes.Where(shape => shape.Selected))
        {
            shape.Color = ViewModel.SelectedColor.ToSKColor();
        }
        VinylCanvasControl.TryInvalidateVisual();
    }
}