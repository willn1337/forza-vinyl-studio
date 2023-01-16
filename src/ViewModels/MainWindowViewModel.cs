using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ForzaVinylStudio.Models;
using ForzaVinylStudio.Models.Tools;
using ForzaVinylStudio.Models.Vinyl;
using Microsoft.Win32;
using SkiaSharp;
using SkiaSharp.Views.WPF;

namespace ForzaVinylStudio.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public ObservableCollection<VinylGroup> VinylGroups { get; } = new();
    public ObservableCollection<Color> ColorSwatches { get; } = new();
    public ObservableCollection<AbstractTool> Tools { get; } = new();

    [ObservableProperty] private AbstractTool currentTool;
    [ObservableProperty] private VinylGroup currentVinylGroup = null!;
    [ObservableProperty] private Color selectedColor = Colors.Black;

    public MainWindowViewModel()
    {
        Tools.Add(new PanTool { Selected = true });
        Tools.Add(new ZoomTool());
        Tools.Add(new SelectTool());
        Tools.Add(new ColorPickerTool());
        Tools.Add(new PaintBucketTool());
        Tools.Add(new ShapeTool());
        Tools.Add(new PolyTool());
        Tools.Add(new TextTool());
        currentTool = Tools[0];
    }

    partial void OnCurrentVinylGroupChanged(VinylGroup value)
    {
        var canvas = App.MainWindow.VinylCanvasControl;
        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        // We can in fact be given a null value
        if (value?.ViewMatrix == SKMatrix.Identity)
        {
            // Assuming we've been given a newly created vinyl group, center it
            // bug: width&height return 0 on startup because of class load order n stuff
            value.ViewMatrix = SKMatrix.CreateTranslation((float)(canvas.ActualWidth / 2), (float)(canvas.ActualHeight / 2));
        }
        canvas.InvalidateVisual();
    }

    public void SetCurrentTool(Type type)
    {
        foreach (var tool in Tools)
        {
            if (tool.GetType() == type)
            {
                tool.Selected = true;
                CurrentTool = tool;
            }
            else
            {
                tool.Selected = false;
            }
        }
    }

    #region Menu bar

    public void OpenVinylGroup(string path, MessageBoxResult? msgBoxResult = null)
    {
        var result = msgBoxResult ?? MessageBox.Show(App.MainWindow,
            "Open vinyl group separately?", path,
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel) return;
        if (result == MessageBoxResult.Yes)
        {
            NewVinylGroup();
            CurrentVinylGroup.Name = Path.GetFileNameWithoutExtension(path);
        }

        using var openStream = File.OpenRead(path);
        var root = JsonSerializer.Deserialize<ForzaPainterJsonModel.Root>(openStream);
        if (root?.shapes == null)
        {
            Utils.ShowErrorMessage("Failed to deserialize json", path);
            return;
        }

        var types = Enum.GetValues<VinylType>();
        foreach (var shape in root.shapes)
        {
            // rounding down to the nearest value
            var type = types.MinBy(type1 => Math.Abs(shape.type - (int)type1));
            var typeIndex = shape.type - ((int)type - 1);
            if (typeIndex == 0)
            {
                typeIndex++; // todo investigate edge case on printstream vinyl group 
            }

            var renderData = VinylRenderData.Get(new VinylInfo(type, typeIndex));
            var newShape = new VinylShape(CurrentVinylGroup, renderData)
            {
                X = shape.data[0],
                Y = -shape.data[1],
                ScaleX = shape.data[2],
                ScaleY = shape.data[3],
                Angle = -shape.data[4] % 360,
                Skew = -shape.data[5],
                Mask = Convert.ToBoolean(shape.data[6]),
                Color = new SKColor((byte)shape.color[0], (byte)shape.color[1], (byte)shape.color[2], (byte)shape.color[3])
            };
            newShape.InvalidateMappedVertices();
            newShape.InvalidateMappedColors();
            newShape.InvalidatePreview();

            CurrentVinylGroup.Shapes.Insert(0,newShape);
        }
    }

    [RelayCommand]
    public void NewVinylGroup()
    {
        var group = new VinylGroup();
        VinylGroups.Add(group);
        CurrentVinylGroup = group;
    }

    [RelayCommand]
    private void OpenVinylGroup()
    {
        var fileDialog = new OpenFileDialog
        {
            Filter = "json files (*.json)|*.json",
            CheckFileExists = true,
            Multiselect = true
        };
        var result = fileDialog.ShowDialog(Application.Current.MainWindow);
        if (result != true) return;

        foreach (var fileName in fileDialog.FileNames)
        {
            OpenVinylGroup(fileName);
        }
    }

    [RelayCommand]
    private void SaveVinylGroup()
    {
    }

    [RelayCommand]
    private void SaveVinylGroupAs()
    {
    }

    [RelayCommand]
    private void CloseVinylGroup(VinylGroup vinylGroup)
    {
        VinylGroups.Remove(vinylGroup);
        // Always having a vinyl group open gives us a lot less to worry about
        if (VinylGroups.Count == 0)
        {
            NewVinylGroup();
        }
    }

    [RelayCommand]
    private void ExportVinylGroup()
    {
        var fileDialog = new SaveFileDialog
        {
            Filter = "json files (*.json)|*.json",
        };
        var result = fileDialog.ShowDialog(Application.Current.MainWindow);
        if (result != true) return;

        // forza-painter's json parser seems to be a little bit "special" so we are forced in to committing this crime against humanity
        var json = new StringBuilder("{\"shapes\":\n[");

        for (var i = CurrentVinylGroup.Shapes.Count - 1; i >= 0; i--)
        {
            var vinyl = CurrentVinylGroup.Shapes[i];

            json.Append(
                @$"{{""type"":{vinyl.RenderData.Info.Get()}, ""data"":[{vinyl.X},{-vinyl.Y},{vinyl.ScaleX},{vinyl.ScaleY},{-vinyl.Angle},{-vinyl.Skew},{Convert.ToInt32(vinyl.Mask)}],""color"":[{vinyl.Color.Red},{vinyl.Color.Green},{vinyl.Color.Blue},{vinyl.Color.Alpha}],""score"":0.0}}" + (i == 0 ? "\n" : ",\n"));
        }

        json.Append("]}");

        File.WriteAllText(fileDialog.FileName, json.ToString());
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void DumpVinyls()
    {
#if DEBUG
        App.DumpVinylModelFiles(CurrentVinylGroup);
#endif
    }
    #endregion

    #region Shapes list

    [RelayCommand]
    private void MoveUp()
    {
        MoveSelectedShape(i => i < 1, -1);
    }

    [RelayCommand]
    private void MoveDown()
    {
        MoveSelectedShape(i => i >= CurrentVinylGroup.Shapes.Count - 1, 1);
    }

    private void MoveSelectedShape(Func<int, bool> guardClause, int n)
    {
        if (CurrentVinylGroup.SelectedShape == null) return;
        var i = CurrentVinylGroup.Shapes.IndexOf(CurrentVinylGroup.SelectedShape);
        if (guardClause(i)) return;
        CurrentVinylGroup.Shapes.Move(i, i + n);
        App.MainWindow.VinylCanvasControl.TryInvalidateVisual();
    }

    #endregion
}