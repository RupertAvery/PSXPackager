using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters;

public class FilenameNormalizationConverter : IValueConverter
{
    private static readonly Regex pbpRegex = new Regex(@"^pbp://(.*?)/disc(\d+)$");


    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string filename && !string.IsNullOrWhiteSpace(filename))
        {
            var match = pbpRegex.Match(filename);
            if (match.Success)
            {
                filename = match.Groups[1].Value ;
                var disc = int.Parse(match.Groups[2].Value) + 1;
                return $"{filename} (Slot {disc})";
            }
            return filename;
        }
        return string.Empty; // Fallback value
    }
    // ConvertBack is not implemented as it's not needed for this converter
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}