using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using ForzaVinylStudio.Models.Vinyl;
using ForzaVinylStudio.Views;
using SkiaSharp;

namespace ForzaVinylStudio;

public partial class App
{
    public new static MainWindow MainWindow { get; private set; } = null!;

    #region Settings
    public class AppSettings
    {
        // Debug
        public bool DrawDebugStrings { get; set; }
        public bool DrawShapeVertices { get; set; }
        public bool DrawHitTestSurface { get; set; }

#if DEBUG
        public AppSettings()
        {
            DrawDebugStrings = true;
            DrawShapeVertices = true;
        }
#endif
    }

    public static AppSettings Settings { get; } = new();
    #endregion

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var fileToOpen = string.Empty;
        for (var i = 0; i != e.Args.Length; ++i)
        {
            var arg = e.Args[i].ToLower();
            if (arg == "-open")
            {
                fileToOpen = e.Args[i + 1];
            }
        }

        MainWindow = new MainWindow();
        if (fileToOpen != string.Empty)
        {
            MainWindow.ViewModel.OpenVinylGroup(fileToOpen, MessageBoxResult.Yes);
        }
        else
        {
            MainWindow.ViewModel.NewVinylGroup();
        }
        MainWindow.Show();
    }

    private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Utils.ShowErrorMessage(e.Exception.ToString(), "I'm sorry, Forza Vinyl Studio encountered something unexpected and has crashed :(");
    }

    // The code used to parse Forza's vinyl shapes. Not pretty, but functional :)
#if DEBUG

    public static void DumpVinylModelFiles(VinylGroup vinylGroup)
    {
        // Expects Vinyls.zip to be extracted to a folder somewhere
        const string vinylsPath = @"D:\!Root\Torrented Stuff\fh5\ForzaHorizon5\media\Livery\Vinyls";

        var stopwatch = Stopwatch.StartNew();

        vinylGroup.Shapes.Clear();
        VinylRenderData.Cache.Clear();

        var jsonSerializerOptions = new JsonSerializerOptions { IgnoreReadOnlyProperties = true };
        var files = Directory.GetFiles(vinylsPath).Where(s => s.EndsWith(".modelbin")).ToArray();

        float xSum = 0, ySum = 0;
        for (var i = 0; i < files.Length; i++)
        {
            var file = files[i];
            var renderData = ParseVinylModelFile(file);
            if (renderData == null || renderData.Info.Type == VinylType.None)
            {
                Debug.WriteLine(i + " Skipping " + file);
                continue;
            }
            
            if (i % 40 == 0 && i >= 40)
            {
                xSum = 0;
                ySum += 255;
                Debug.WriteLine(i);
            }

            var dir = Directory.CreateDirectory("Resources/Vinyls/" + Enum.GetName(renderData.Info.Type)!);
            // todo we are using an insulting amount of disk space
            var json = JsonSerializer.Serialize(renderData, jsonSerializerOptions);
            File.WriteAllText(Path.Combine(dir.FullName, $"{renderData.Info.TypeIndex}"), json);
            VinylRenderData.Cache.Add(renderData);

            var vinyl = new VinylShape(vinylGroup, renderData);
            vinyl.InvalidateMappedVertices();
            var bounds = vinyl.BoundingBox;
            vinyl.X = xSum - bounds.Left;
            vinyl.Y = ySum;
            vinyl.Color = SKColor.FromHsv(((float)i / files.Length) * 360f, 100, 100);
            vinylGroup.Shapes.Insert(0, vinyl);
            xSum += bounds.Width + 20;
        }

        Debug.WriteLine(stopwatch.ElapsedMilliseconds);
    }
     
    private static VinylRenderData? ParseVinylModelFile(string path)
    {
        var src = File.ReadAllBytes(path);

        void Log(object o)
        {
            Debug.WriteLine($"{o} ({path})");
        }

        var facesPattern = new byte[] { 0x02, 0x000, 0x001, 0x000, 0x039, 0x000, 0x000, 0x000 };
        var facesLocation = PatternSearch(src, facesPattern);
        if (facesLocation < 0)
        {
            Log("Couldn't find faces pattern");
            return null;
        }

        var faceCount = BitConverter.ToInt32(src, facesLocation - 8);
        if (faceCount is < 3 or > ushort.MaxValue)
        {
            Log("Unusual face count: " + faceCount);
        }

        var faces = new ushort[faceCount];
        var faceLocation = facesLocation + facesPattern.Length;
        for (var i = 0; i < faceCount; i++, faceLocation += 2)
        {
            faces[i] = BitConverter.ToUInt16(src, faceLocation);
            if (faceCount is < 0 or > ushort.MaxValue)
            {
                Log($"Unusual face @ {i}: {faces[i]}");
            }
        }

        var verticesPattern = new byte[] { 0x001, 0x000, 0x0D, 0x000, 0x000, 0x000 };
        var verticesLocation = PatternSearch(src, verticesPattern);
        if (verticesLocation < 0)
        {
            Log("Couldn't find vertices pattern");
            return null;
        }

        var vertexCount = BitConverter.ToInt32(src, verticesLocation - 10);
        if (vertexCount is < 3 or > ushort.MaxValue)
        {
            Log("Unusual vertex count: " + vertexCount);
        }

        var vertices = new SKPoint[vertexCount];
        var vertexLocation = verticesLocation + verticesPattern.Length;
        for (var i = 0; i < vertexCount; i++, vertexLocation += 8) // sizeof VertexBlock = 8
        {
            // the vertex positions are in the value range of signed short, but the Forza vinyl editor appears to divide that by 510. (source: came to me in a dream)
            const float scaleDivisor = 510;
            var x = BitConverter.ToInt16(src, vertexLocation) / scaleDivisor;
            var y = -(BitConverter.ToInt16(src, vertexLocation + 2) / scaleDivisor);
            if (x is < -65f or > 65f || y is < -65f or > 65f)
            {
                Log($"Unusual vertex @ {i}: {x},{y}");
            }

            var scaleLocation = facesLocation - 41;
            var scaleX = BitConverter.ToSingle(src, scaleLocation);
            var scaleY = BitConverter.ToSingle(src, scaleLocation + 4);
            if (scaleX is <= 0f or >= 2f || scaleY is <= 0f or >= 2f)
            {
                Log($"Unusual scale: {scaleX},{scaleY}");
            }

            // for whatever reason Forza considers 0.25 == 1
            scaleX *= 4f;
            scaleY *= 4f;

            var scaledMatrix = SKMatrix.CreateScale(scaleX, scaleY);
            vertices[i] = scaledMatrix.MapPoint(x, y);
        }

        var uvPattern = new byte[] { 0x04, 0x00, 0x25, 0x00, 0x00, 0x00 };
        var uvLocation = PatternSearch(src, uvPattern);
        if (uvLocation < 0)
        {
            Log("Couldn't find uv pattern");
            return null;
        }

        var uvCount = BitConverter.ToInt32(src, uvLocation - 10);
        if (uvCount is < 3 or > ushort.MaxValue)
        {
            Log("Unusual uv count: " + uvCount);
        }

        var verticesAlpha = new byte[uvCount];
        var alphaLocation = uvLocation + uvPattern.Length + 15;
        for (var i = 0; i < uvCount; i++, alphaLocation += 16)
        {
            verticesAlpha[i] = src[alphaLocation];
        }

        var name = Path.GetFileNameWithoutExtension(path);
        var split = name.Split('_');
        var character = split[0];
        var index = Convert.ToInt32(split[1]); // !!! Index starts from 1 !!!
        var wrapIndex = index > 40; // Community vinyls go up to 80 for some reason (encompassing two sets)
        if (wrapIndex)
        {
            // More wackiness: Community vinyls 4 start at index 51
            if (character == "V" && index > 40)
            {
                index -= 10;
            }
            index -= 40;
        }

        var type = character switch
        {
            // PGG presents: The Alphabet (missing J,Y,Z)
            "A" => VinylType.Primitives,
            "B" => VinylType.Gradient_Shapes,
            "C" => VinylType.Stripes,
            "D" => VinylType.Tears,
            "E" => VinylType.Racing_Icons,
            "F" => VinylType.Flames,
            "G" => VinylType.Paint_Splats,
            "H" => VinylType.Tribal,
            "I" => VinylType.Nature,
            "K" => VinylType.None, // Unused?
            "KK" => VinylType.Upper_Letters_5,
            "LL" => VinylType.Lower_Letters_5,
            "L" => VinylType.None, // Also unused?
            "M" => VinylType.Upper_Letters_2,
            "MM" => VinylType.Upper_Letters_6,
            "NN" => VinylType.Lower_Letters_6,
            "N" => VinylType.Lower_Letters_2,
            "OO" => VinylType.Upper_Letters_7,
            "O" => VinylType.Upper_Letters_3,
            "PP" => VinylType.Lower_Letters_7,
            "P" => VinylType.Lower_Letters_3,
            "QQ" => VinylType.Upper_Letters_8,
            "Q" => VinylType.Upper_Letters_4,
            "RR" => VinylType.Lower_Letters_8,
            "R" => VinylType.Lower_Letters_4,
            "SS" => VinylType.Upper_Letters_9,
            "S" => VinylType.Upper_Letters_1,
            "TT" => VinylType.Lower_Letters_9,
            "T" => VinylType.Lower_Letters_1,
            "UU" => VinylType.Upper_Letters_10,
            "U" => wrapIndex ? VinylType.Community_Vinyls_2 : VinylType.Community_Vinyls_1,
            "VV" => VinylType.Lower_Letters_10,
            "V" => wrapIndex ? VinylType.Community_Vinyls_4 : VinylType.Community_Vinyls_3,
            "WW" => VinylType.Upper_Letters_11,
            "XX" => VinylType.Lower_Letters_11,
            _ => VinylType.None
        };

        return new VinylRenderData(new VinylInfo(type, index), vertices, faces, verticesAlpha);
    }


    // https://stackoverflow.com/a/38625726/9286324
    private static int PatternSearch(byte[] src, byte[] pattern)
    {
        int maxFirstCharSlot = src.Length - pattern.Length + 1;
        for (int i = 0; i < maxFirstCharSlot; i++)
        {
            if (src[i] != pattern[0]) // compare only first byte
                continue;

            // found a match on first byte, now try to match rest of the pattern
            for (int j = pattern.Length - 1; j >= 1; j--)
            {
                if (src[i + j] != pattern[j]) break;
                if (j == 1) return i;
            }
        }
        return -1;
    }
#endif
}