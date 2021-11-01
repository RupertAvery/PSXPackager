using System;
using System.Windows.Data;
using System.Windows.Media;

namespace PSXPackagerGUI.Converters
{
    class HasErrorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value)
                ? Brushes.Red
                : Brushes.ForestGreen;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}