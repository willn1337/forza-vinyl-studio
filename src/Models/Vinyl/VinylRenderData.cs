using SkiaSharp;
using System.Text.Json;
using System;
using System.IO;
using System.Collections.Generic;

namespace ForzaVinylStudio.Models.Vinyl;

public record VinylRenderData(VinylInfo Info, SKPoint[] Vertices, ushort[] Indices, byte[] VerticesAlpha)
{   
    public static readonly List<VinylRenderData> Cache = new();

    public override string ToString()
    {
        return $"{nameof(Vertices)}: {Vertices.Length}, {nameof(Indices)}: {Indices.Length}";
    }

    public static VinylRenderData Get(VinylInfo info)
    {
        var renderData = Cache.Find(data => data.Info.Equals(info));
        if (renderData != null) return renderData;

        var path = $"Resources/Vinyls/{info.Type}/{info.TypeIndex}";
        renderData = JsonSerializer.Deserialize<VinylRenderData>(File.ReadAllText(path));
        if (renderData == null)
        {
            throw new Exception("Failed render data serialization on " + path);
        }
        Cache.Add(renderData);

        return renderData;
    }
}