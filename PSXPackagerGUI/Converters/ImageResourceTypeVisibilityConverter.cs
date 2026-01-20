using System;
using System.Windows;
using System.Windows.Data;
using Popstation.Pbp;

namespace PSXPackagerGUI.Converters;

public class ImageResourceTypeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return (ResourceType)value switch
        {
            ResourceType.ICON0 => Visibility.Visible,
            ResourceType.ICON1 => Visibility.Hidden,
            ResourceType.PIC0 => Visibility.Visible,
            ResourceType.PIC1 => Visibility.Visible,
            ResourceType.SND0 => Visibility.Hidden,
            ResourceType.BOOT => Visibility.Visible,
            _ => Visibility.Hidden
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}