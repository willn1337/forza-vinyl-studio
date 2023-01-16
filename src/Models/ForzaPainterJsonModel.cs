#pragma warning disable
// ReSharper disable all
namespace ForzaVinylStudio.Models;

public class ForzaPainterJsonModel
{
    public class Root
    {
        public Shape[] shapes { get; set; }
    }

    public class Shape
    {
        public int type { get; set; }
        public float[] data { get; set; }
        public int[] color { get; set; }
    }
}