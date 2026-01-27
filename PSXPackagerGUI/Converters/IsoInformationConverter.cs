using PSXPackagerGUI.Models;
using System;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters
{
    class IsoInformationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }
            if (value is Disc disc)
            {
                if (disc.IsEmpty)
                {
                    return "No disc loaded";
                }
                else
                {
                    var title = disc.Title;
                    var sizef = disc.Size / 1048576f;

                    return $"{title} ({sizef:F2}MB)";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}