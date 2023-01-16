using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using ForzaVinylStudio.Models.Tools;
using ForzaVinylStudio.Models.Vinyl;
using ForzaVinylStudio.ViewModels.Tools;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace ForzaVinylStudio.Views.Controls;

public class VinylCanvasControl : SKGLElement
{
    private static VinylGroup VinylGroup => App.MainWindow.ViewModel.CurrentVinylGroup;
    private static AbstractTool Tool => App.MainWindow.ViewModel.CurrentTool;

    #region Paint objects
    public static readonly SKPaint DefaultPaint = new();
    private static readonly SKPaint HoverPaint = new() { Color = SKColors.CornflowerBlue };
    private static readonly SKPaint SelectionPaint = new() { Color = SKColors.CornflowerBlue.WithAlpha(128) };
    private static readonly SKPaint GridPaint = new() { Color = SKColors.DarkGray, IsStroke = true };
    private static readonly SKPaint OutlinePaint = new() { Color = SKColors.Gray, IsStroke = true };
    private static readonly SKPaint DebugPaint = new() { Color = SKColors.Blue.WithAlpha(128), StrokeCap = SKStrokeCap.Round, StrokeWidth = 1 };
    #endregion

    private readonly Stopwatch frameTimeStopwatch = new();

    // Mouse position relative to the control
    public SKPoint MousePos;
    // Mouse position relative to 0,0 in world space
    public SKPoint MappedMousePos;
    public SKPoint PrevMappedMousePos;
    public SKPoint MouseDownPos;
    public SKPoint MappedMouseDownPos;
    public SKPoint MouseDragPos;
    public SKPoint MappedMouseDragPos;
    public VinylShape? HoverShape;
    public SKRect? SelectionRect;
    // In some cases (like clicking off a popup) the MouseMove event will fire before MouseDown, messing things up. 
    public bool MouseActuallyDown;

    public SKSurface? HitTestSurface;

    private bool dirtyRenderSize;
    private SKSurface? surface;
    private long lastInvalidation;
    private AbstractTool? prevTool;

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        dirtyRenderSize = true;
        base.OnRenderSizeChanged(sizeInfo);
    }

    protected override void OnPaint(TimeSpan e)
    {
        if (App.Settings.DrawDebugStrings)
        {
            frameTimeStopwatch.Restart();
        }
        base.OnPaint(e);
    }

    protected override void OnPaintSurface(SKPaintGLSurfaceEventArgs e)
    {
        surface = e.Surface;

        if (HitTestSurface == null || dirtyRenderSize)
        {
            dirtyRenderSize = false;
            HitTestSurface?.Dispose();
            HitTestSurface = SKSurface.Create(surface.Context, true, e.Info, surface.SurfaceProperties);
        }

        var canvas = surface.Canvas;
        var hitTestCanvas = HitTestSurface.Canvas;

        canvas.Clear(VinylGroup.BackgroundColor);
        HitTestSurface.Canvas.Clear();

        canvas.SetMatrix(VinylGroup.ViewMatrix);
        hitTestCanvas.SetMatrix(VinylGroup.ViewMatrix);

        for (var i = VinylGroup.Shapes.Count - 1; i >= 0; i--)
        {
            var shape = VinylGroup.Shapes[i];
            if (!shape.Visible) continue;

            var bounds = shape.BoundingBox;

            if (HoverShape == shape)
            {
                DrawShape(canvas, shape, null, HoverPaint);
            }
            else
            {
                DrawShape(canvas, shape, shape.MappedColors, DefaultPaint);
            }

            // Draw the hit test shapes only if we're in plausible range of clicking on it
            if (bounds.Contains(MappedMousePos) || SelectionRect?.Standardized.IntersectsWith(bounds) == true)
            {
                DrawShape(hitTestCanvas, shape, shape.HitTestColors, DefaultPaint);
            }

            if (shape.Selected)
            {
                if (App.Settings.DrawShapeVertices)
                {
                    canvas.DrawPoints(SKPointMode.Points, shape.MappedVertices, DebugPaint);
                }

                canvas.DrawRect(shape.BoundingBox, OutlinePaint);
                canvas.DrawCircle(shape.X, shape.Y, 5 * (shape.ScaleX + shape.ScaleY), OutlinePaint);
            }
        }

        if (Tool is ShapeTool shapeTool)
        {
            var renderData = VinylRenderData.Get(shapeTool.ViewModel.SelectedVinyl.VinylInfo);
            var matrix = SKMatrix.CreateTranslation(MappedMousePos.X, MappedMousePos.Y);
            var vertices = matrix.MapPoints(renderData.Vertices);
            canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, null, renderData.Indices, SelectionPaint);
        }

        // todo proper grid
        const int infinity = 8192;
        const int step = 64;
        for (var i = -infinity; i < infinity; i += step)
        {
            if (i == 0) GridPaint.Color = SKColors.Black.WithAlpha(128);
            canvas.DrawLine(-infinity, i, infinity, i, GridPaint);
            canvas.DrawLine(i, -infinity, i, infinity, GridPaint);
            if (i == 0) GridPaint.Color = SKColors.Gray.WithAlpha(128);
        }

        if (SelectionRect != null)
        {
            canvas.DrawRect(SelectionRect.Value, SelectionPaint);
        }

        if (Tool is SelectTool selectTool && selectTool.ShapesTransformRect != null)
        {
            canvas.DrawRect(selectTool.ShapesTransformRect.Value, OutlinePaint);

            var radius = selectTool.ShapeTransformPointRadius();
            foreach (var pt in selectTool.ShapeTransformPoints())
            {
                var dist = SKPoint.Distance(MappedMousePos, pt);
                var paint = DefaultPaint;
                if (dist <= radius)
                {
                    paint = SelectionPaint;
                }

                canvas.DrawCircle(pt, radius, paint);
                canvas.DrawCircle(pt, radius, OutlinePaint);
            }
        }

        canvas.ResetMatrix();

        if (App.Settings.DrawHitTestSurface)
        {
            HitTestSurface.Draw(canvas, 0, 0, DebugPaint);
        }

        if (App.Settings.DrawDebugStrings)
        {
            var debugStrings = new List<string>
            {
                $"{nameof(frameTimeStopwatch)}: {frameTimeStopwatch.Elapsed.TotalMilliseconds}ms",
                $"{nameof(VinylGroup.ViewMatrix)}: {VinylGroup.ViewMatrix.AsString()}",
                $"{nameof(MousePos)}: {MousePos}",
                $"{nameof(MappedMousePos)}: {MappedMousePos}",
                $"{nameof(SelectionRect)}: {SelectionRect}",
            };

            if (HoverShape != null)
            {
                debugStrings.Add(nameof(HoverShape));
                debugStrings.Add(HoverShape.RenderData.Info.ToString());
                debugStrings.Add(HoverShape.RenderData.ToString());
                debugStrings.Add(HoverShape.ToString());
            }

            for (var i = 0; i < debugStrings.Count; i++)
            {
                var text = debugStrings[i];
                canvas.DrawText(text, 0, DebugPaint.FontSpacing + i * DebugPaint.FontSpacing, DebugPaint);
            }
        }
    }

    public static void DrawShape(SKCanvas canvas, VinylShape shape, SKColor[]? colors, SKPaint paint)
    {
        canvas.DrawVertices(SKVertexMode.Triangles, shape.MappedVertices, null, colors, shape.RenderData.Indices, paint);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        MouseActuallyDown = true;
        MouseDownPos = e.GetPosition(this).ToSKPoint();
        MouseDragPos = MouseDownPos;
        MappedMouseDownPos = VinylGroup.ViewMatrix.Invert().MapPoint(MouseDownPos);
        MappedMouseDragPos = MappedMouseDownPos;

        Tool.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Middle)
        {
            prevTool = Tool;
            App.MainWindow.ViewModel.SetCurrentTool(typeof(PanTool));
        }

        TryInvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        MousePos = e.GetPosition(this).ToSKPoint();
        PrevMappedMousePos = MappedMousePos;
        MappedMousePos = VinylGroup.ViewMatrix.Invert().MapPoint(MousePos);
        Tool.OnMouseMove(e);
        if (MouseActuallyDown)
        {
            MouseDragPos = MousePos;
            MappedMouseDragPos = MappedMousePos;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        MouseActuallyDown = false;

        Tool.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Middle)
        {
            App.MainWindow.ViewModel.SetCurrentTool(prevTool!.GetType());
        }

        TryInvalidateVisual();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        Tool.OnMouseWheel(e);
        const float zoomStep = .8f;
        Zoom(e.Delta > 0 ? 1f / zoomStep : zoomStep);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            for (var i = VinylGroup.Shapes.Count - 1; i >= 0; i--)
            {
                if (VinylGroup.Shapes[i].Selected)
                {
                    VinylGroup.Shapes.RemoveAt(i);
                }
            }

            VinylGroup.SelectedShape = null;
            TryInvalidateVisual();
        }
        else if (e.Key == Key.R && Tool is SelectTool tool)
        {
            foreach (var shape in VinylGroup.Shapes.Where(shape => shape.Selected))
            {
                shape.Angle += 5;
            }

            TryInvalidateVisual();
            tool.InvalidateShapesTransformRect();
        }
        else if (e.Key == Key.P)
        {
            foreach (var shape in VinylGroup.Shapes)
            {
                shape.InvalidatePreview();
            }
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        TryInvalidateVisual();
    }

    public void TryInvalidateVisual()
    {
        var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        if (now - lastInvalidation < 8) return; // todo make this a setting
        lastInvalidation = now;
        InvalidateVisual();
    }

    public void Zoom(float scale)
    {
        VinylGroup.ViewMatrix = SKMatrix.Concat(SKMatrix.CreateScale(scale, scale, MousePos.X, MousePos.Y), VinylGroup.ViewMatrix);
        TryInvalidateVisual();
    }

}