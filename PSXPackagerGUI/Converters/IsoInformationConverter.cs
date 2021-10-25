using System;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters
{
    class IsoInformationConverter : IMultiValueConverter
    {
        public object Convert(object[] value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value[0] == null || value[0].GetType() != typeof(string))
            {
                return null;
            }
            if (value[1] == null)
            {
                return null;
            }
            var title = (string)value[0];
            var size = (uint)value[1];
            var sizef = size / 1048576f;

            return $"{title} ({sizef:F2}MB)";
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}