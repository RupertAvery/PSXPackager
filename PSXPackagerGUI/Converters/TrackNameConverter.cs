using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Converters;

public class TrackNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Track track)
        {
            var fileName = FilenameNormalizer.Normalize(track.CueTrack.FileEntry.FileName);
            var trackNo = track.Number;
            return $"Track {trackNo} - {fileName}";
        }
        return string.Empty; // Fallback value
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}