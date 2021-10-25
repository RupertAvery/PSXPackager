using System;
using System.Windows.Data;
using System.Windows.Media;

namespace PSXPackagerGUI.Converters
{
    class IsEmptyBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value)
                ? Brushes.Gray
                : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
