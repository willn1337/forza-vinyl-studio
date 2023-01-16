using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ForzaVinylStudio.Views.Converters;

// https://stackoverflow.com/a/21939778/9286324
public class NullVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Hidden : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}