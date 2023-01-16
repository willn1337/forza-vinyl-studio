using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using ForzaVinylStudio.Models.Tools;
using ForzaVinylStudio.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;

namespace ForzaVinylStudio.Models.Vinyl;

public partial class VinylShape : ObservableObject
{
    public VinylRenderData RenderData { get; set; }

    // Cached values for faster rendering
    public SKPoint[] MappedVertices { get; }
    public SKColor[] MappedColors { get; }
    public SKColor[] HitTestColors { get; }
    public SKRect BoundingBox { get; private set; }

    [ObservableProperty] private SKColor color = SKColors.White;
    [ObservableProperty] private SKMatrix matrix = SKMatrix.Identity;
    [ObservableProperty] private bool visible = true;
    [ObservableProperty] private bool selected = true;
    [ObservableProperty] private ImageSource? preview;
    [ObservableProperty] private bool mask;
    [ObservableProperty] private float angle;
    [ObservableProperty] private float x;
    [ObservableProperty] private float y;
    [ObservableProperty] private float scaleX = 1;
    [ObservableProperty] private float scaleY = 1;
    [ObservableProperty] private float skew;
    
    public VinylShape(VinylGroup vinylGroup, VinylRenderData renderData)
    {
        RenderData = renderData;
        MappedVertices = new SKPoint[renderData.Vertices.Length];
        MappedColors = new SKColor[renderData.VerticesAlpha.Length];
        HitTestColors = new SKColor[renderData.Vertices.Length];

        SKColor hitTestColor;
        do
        {
            var randColor = new byte[3];
            Random.Shared.NextBytes(randColor);
            hitTestColor = new SKColor(randColor[0], randColor[1], randColor[2]);
            // Make sure we aren't any of these colors
        } while (hitTestColor == SKColors.Black || vinylGroup.BackgroundColor == hitTestColor || vinylGroup.Shapes.Any(shape => shape.HitTestColors[0] == hitTestColor));
        Array.Fill(HitTestColors, hitTestColor);
    }

    public void InvalidateMappedVertices()
    {
        matrix.MapPoints(MappedVertices, RenderData.Vertices);

        var left = MappedVertices.Min(p => p.X);
        var top = MappedVertices.Min(p => p.Y);
        var right = MappedVertices.Max(p => p.X);
        var bottom = MappedVertices.Max(p => p.Y);
        BoundingBox = new SKRect(left, top, right, bottom);
    }

    public void InvalidateMappedColors()
    {
        for (var i = 0; i < MappedColors.Length; i++)
        {
            MappedColors[i] = color.WithAlpha(Math.Min(RenderData.VerticesAlpha[i], color.Alpha));
        }
        InvalidatePreview();
    }

    // Expensive, don't call this often!
    public void InvalidatePreview()
    {
        var width = (int)Math.Max(BoundingBox.Width, 1);
        var height = (int)Math.Max(BoundingBox.Height, 1);
        var imageInfo = new SKImageInfo(width, height);
        var surface = SKSurface.Create(imageInfo);
        var canvas = surface.Canvas;
        canvas.Translate(-X + (width / 2f), -Y + (height / 2f));
        VinylCanvasControl.DrawShape(canvas, this, MappedColors, VinylCanvasControl.DefaultPaint);
        var pixels = surface.PeekPixels();
        var bmp = pixels.ToWriteableBitmap();
        Preview = bmp;
    }

    // ReSharper disable UnusedParameterInPartialMethod
    partial void OnSelectedChanged(bool value)
    {
        if (value)
        {
            App.MainWindow.ViewModel.SelectedColor = Color.ToColor();
        }
    }

    partial void OnColorChanged(SKColor value)
    {
        InvalidateMappedColors();
    }

    partial void OnMatrixChanged(SKMatrix value)
    {
        InvalidateMappedVertices();
    }

    partial void OnXChanged(float value) => MatrixTransformChanged();
    partial void OnYChanged(float value) => MatrixTransformChanged();
    partial void OnAngleChanged(float value) => MatrixTransformChanged();
    partial void OnScaleXChanged(float value) => MatrixTransformChanged();
    partial void OnScaleYChanged(float value) => MatrixTransformChanged();
    partial void OnSkewChanged(float value) => MatrixTransformChanged();

    // ReSharper restore UnusedParameterInPartialMethod

    private void MatrixTransformChanged()
    {
        var translation = SKMatrix.CreateTranslation(x, y);
        var scale = SKMatrix.CreateScale(scaleX, scaleY);
        var rotation = SKMatrix.CreateRotationDegrees(angle);
        var skew = SKMatrix.CreateSkew(this.skew, 0);
        Matrix = scale.PostConcat(skew).PostConcat(rotation).PostConcat(translation);
        App.MainWindow.VinylCanvasControl.TryInvalidateVisual();
        if (App.MainWindow.ViewModel.CurrentTool is SelectTool tool)
        {
            tool.InvalidateShapesTransformRect();
        }
    }

    public override string ToString()
    {
        return $"{nameof(x)}: {x}, {nameof(y)}: {y}, {nameof(angle)}: {angle}, {nameof(scaleX)}: {scaleX}, {nameof(scaleY)}: {scaleY}, {nameof(skew)}: {skew}, {nameof(mask)}: {mask}, {nameof(color)}: {color}";
    }
}
