using System.Windows;
using SkiaSharp;

namespace ForzaVinylStudio;

// Kitchen sink utility class
public static class Utils
{
    public static void ShowErrorMessage(string message, string? caption = null)
    {
        MessageBox.Show(Application.Current.MainWindow!, message,  caption ?? "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public static string AsString(this SKMatrix matrix)
    {
        return string.Join(", ", matrix.Values);
    }

    public static SKPoint ToSKPoint(this Point point)
    {
        return new SKPoint((float)point.X, (float)point.Y);
    }
}