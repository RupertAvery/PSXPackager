using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters;

public class NotNullVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null ? Visibility.Visible : Visibility.Hidden;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}