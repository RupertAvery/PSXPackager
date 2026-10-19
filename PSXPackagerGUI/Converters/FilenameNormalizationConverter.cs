using System;
using System.Globalization;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters;

public class FilenameNormalizationConverter : IValueConverter
{


    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string filename && !string.IsNullOrWhiteSpace(filename))
        {
            return FilenameNormalizer.Normalize(filename);
        }
        return string.Empty; // Fallback value
    }
    // ConvertBack is not implemented as it's not needed for this converter
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}