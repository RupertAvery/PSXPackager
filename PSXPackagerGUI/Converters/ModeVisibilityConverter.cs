using System;
using System.Windows;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters
{
    class ModeVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            return ((AppMode)value[0]) == ((AppMode)value[1]) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}