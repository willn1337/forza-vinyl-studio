using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Media;
using ForzaVinylStudio.Models.Vinyl;
using SkiaSharp;
using ForzaVinylStudio.ViewModels.Tools;

namespace ForzaVinylStudio.Models.Tools;

// As it turns out, shape transformations are a bit of a nightmare.
// See https://github.dev/konvajs/konva/blob/6fb7a0669d202d3922b9764d50d02efcef0f1624/src/shapes/Transformer.ts
// Or https://github.dev/sk1project/sk1-wx/blob/62cc98e0a1999f754c6ceb4d728fb10db1bfa434/src/sk1/document/controllers/trafo_ctrl.py
// If (You) want to help implement this, i'd be very grateful! Until then, we'll have to stay simple :')
public class SelectTool : AbstractTool
{
    public override ImageSource Icon { get; } = GetIcon("cursor");
    public override string Name => "Selection";
    public override AbstractToolViewModel ViewModel { get; } = new SelectToolViewModel();

    private readonly Cursor rotateCursor = GetCursor("arrow-circle-double");

    /// <summary>
    /// Rectangle that encompasses selected shapes
    /// </summary>
    public SKRect? ShapesTransformRect;

    private bool draggingTransformPoint;
    private float prevDist;

    public override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        if (ShapesTransformRect != null)
        {
            var pts = ShapeTransformPoints();
            var radius = ShapeTransformPointRadius();
            var point = pts.FirstOrDefault(point => SKPoint.Distance(Canvas.MappedMouseDownPos, point) <= radius);
            if (point != SKPoint.Empty)
            {
                draggingTransformPoint = true;
                return;
            }
        }

        if (Canvas.HoverShape == null)
        {
            if (Cursor != rotateCursor)
            {
                // We clicked on nothing, so start dragging a selection box
                Canvas.SelectionRect = SKRect.Create(Canvas.MappedMouseDownPos, SKSize.Empty);
                // ... and deselect everything else
                foreach (var shape in VinylGroup.Shapes)
                {
                    shape.Selected = false;
                }
                ShapesTransformRect = null;
                VinylGroup.SelectedShape = null;
            }
        }
        else
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                Canvas.HoverShape.Selected = !Canvas.HoverShape.Selected;
                VinylGroup.SelectedShape = Canvas.HoverShape.Selected ? Canvas.HoverShape : VinylGroup.Shapes.First(shape => shape.Selected);
            }
            else
            {
                // If it's already selected, we pass this statement and begin dragging the shape around
                if (!Canvas.HoverShape.Selected)
                {
                    // We clicked on another shape, but was not holding Ctrl.
                    // So, deselect all shapes
                    foreach (var shape in VinylGroup.Shapes)
                    {
                        shape.Selected = false;
                    }
                    // And select our new shape
                    Canvas.HoverShape.Selected = true;
                    VinylGroup.SelectedShape = Canvas.HoverShape;
                }
            }

            InvalidateShapesTransformRect();
        }

        Canvas.TryInvalidateVisual();
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        Canvas.HoverShape = HitTest();

        var newCursor = Cursors.Arrow;
        if (ShapesTransformRect != null)
        {
            var rect = ShapesTransformRect.Value;

            var radius = ShapeTransformPointRadius();
            rect.Inflate(radius, radius);

            if (!rect.Contains(Canvas.MappedMousePos)
                && !draggingTransformPoint)
            {
                //newCursor = rotateCursor; // todo
            }
        }
        Cursor = newCursor;

        if (e.LeftButton == MouseButtonState.Pressed && Canvas.MouseActuallyDown)
        {
            if (draggingTransformPoint)
            {
                var center = new SKPoint(ShapesTransformRect!.Value.MidX, ShapesTransformRect.Value.MidY);
                prevDist = SKPoint.Distance(center, Canvas.PrevMappedMousePos);
                var dist = SKPoint.Distance(center, Canvas.MappedMousePos);
                // If our new distance is greater than our previous distance, this means
                // we are dragging away from the center, therefore increasing the size, and vice versa.
                var delta = dist - prevDist;
                foreach (var shape in VinylGroup.Shapes.Where(shape => shape.Selected))
                {
                    var f = delta / 128f; // dividing by arbitrary value, seems to work well enough (lie)
                    shape.ScaleX += shape.ScaleX < 0f ? -f : f;
                    shape.ScaleY += shape.ScaleY < 0f ? -f : f;
                }
                InvalidateShapesTransformRect();
            }
            else
            {
                if (Canvas.SelectionRect != null)
                {
                    Canvas.SelectionRect = SKRect.Create(Canvas.SelectionRect.Value.Location, new SKSize(SKPoint.Subtract(Canvas.MappedMousePos, Canvas.SelectionRect.Value.Location)));
                }
                else
                {
                    if (Cursor == rotateCursor)
                    {
                        // todo
                    }
                    else
                    {
                        foreach (var shape in VinylGroup.Shapes.Where(shape => shape.Selected))
                        {
                            shape.X += Canvas.MappedMousePos.X - Canvas.MappedMouseDragPos.X;
                            shape.Y += Canvas.MappedMousePos.Y - Canvas.MappedMouseDragPos.Y;
                        }

                        InvalidateShapesTransformRect();
                    }
                }
            }
        }

        Canvas.TryInvalidateVisual();
    }

    public override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;

        draggingTransformPoint = false;
        foreach (var shape in VinylGroup.Shapes.Where(shape => shape.Selected))
        {
            shape.InvalidatePreview();
        }

        if (Canvas.SelectionRect != null)
        {
            var hitShapes = HitTest(Canvas.SelectionRect.Value).ToList();
            if (hitShapes.Count > 0)
            {
                hitShapes.ForEach(shape => shape.Selected = true);
                InvalidateShapesTransformRect();
                VinylGroup.SelectedShape = hitShapes[0];
            }

            Canvas.SelectionRect = null;
            Canvas.TryInvalidateVisual();
        }
    }

    public static VinylShape? HitTest()
    {
        return HitTest(SKRect.Create(Canvas.MappedMousePos.X, Canvas.MappedMousePos.Y, 1, 1)).FirstOrDefault();
    }

    private static IEnumerable<VinylShape> HitTest(SKRect rect)
    {
        // we cannot input a negative rect because we are reading a bitmap
        var standardized = VinylGroup.ViewMatrix.MapRect(rect).Standardized;
        var x = (int)standardized.Left;
        var y = (int)standardized.Top;
        var width = (int)Math.Max(standardized.Width, 1);
        var height = (int)Math.Max(standardized.Height, 1);

        var imgInfo = new SKImageInfo(width, height, SKColorType.Rgba8888);
        var hitTestSurfaceBuffer = new byte[imgInfo.Width * imgInfo.Height * imgInfo.BytesPerPixel];

        // https://stackoverflow.com/a/537722/9286324
        var pinnedArray = GCHandle.Alloc(hitTestSurfaceBuffer, GCHandleType.Pinned);
        Canvas.HitTestSurface!.ReadPixels(imgInfo, pinnedArray.AddrOfPinnedObject(), imgInfo.RowBytes, x, y);
        pinnedArray.Free();

        var hitColors = new List<SKColor>();
        for (var i = 0; i < hitTestSurfaceBuffer.Length; i += imgInfo.BytesPerPixel)
        {
            var r = hitTestSurfaceBuffer[i];
            var g = hitTestSurfaceBuffer[i + 1];
            var b = hitTestSurfaceBuffer[i + 2];
            if (r == 0 && g == 0 && b == 0) continue;
            var color = new SKColor(r, g, b);
            // we've already returned this shape, move on
            if (hitColors.Contains(color)) continue;
            // does the color match any of our shape's colors?
            var shape = VinylGroup.Shapes.FirstOrDefault(shape => shape.HitTestColors[0] == color);
            if (shape == null || !shape.Visible) continue;
            // match! return it
            hitColors.Add(color);
            yield return shape;
        }
    }

    public void InvalidateShapesTransformRect()
    {
        var selectedShapes = VinylGroup.Shapes.Where(vinyl => vinyl.Selected).ToArray();
        if (selectedShapes.Length <= 0)
        {
            ShapesTransformRect = null;
            return;
        }

        var left = selectedShapes.Min(vinyl => vinyl.BoundingBox.Left);
        var top = selectedShapes.Min(vinyl => vinyl.BoundingBox.Top);
        var right = selectedShapes.Max(vinyl => vinyl.BoundingBox.Right);
        var bottom = selectedShapes.Max(vinyl => vinyl.BoundingBox.Bottom);
        ShapesTransformRect = new SKRect(left, top, right, bottom);
    }

    public SKPoint[] ShapeTransformPoints()
    {
        var r = ShapesTransformRect!.Value;
        var topLeft = r.Location;
        var topRight = new SKPoint(r.Right, r.Top);
        var bottomRight = new SKPoint(r.Right, r.Bottom);
        var bottomLeft = new SKPoint(r.Left, r.Bottom);

        return new[]
            {
                topLeft,
                topRight,
                bottomRight,
                bottomLeft,
            };
    }

    public float ShapeTransformPointRadius() => 10f / VinylGroup.ViewMatrix.ScaleX;
}