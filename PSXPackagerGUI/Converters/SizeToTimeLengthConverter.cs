using System;
using System.Globalization;
using System.Windows.Data;

namespace PSXPackagerGUI.Converters;

[ValueConversion(typeof(long), typeof(string))]
public class SizeToTimeLengthConverter : IValueConverter
{
    private const int BytesPerSector = 2352;
    private const int FramesPerSecond = 75;

    // Convert: Long (Bytes) -> String (mm:ss:ff)
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytePosition && bytePosition >= 0)
        {
            long totalSectors = bytePosition / BytesPerSector;
            long frames = totalSectors % FramesPerSecond;
            long totalSeconds = totalSectors / FramesPerSecond;
            long seconds = totalSeconds % 60;
            long minutes = totalSeconds / 60;

            return $"{minutes:D2}:{seconds:D2}:{frames:D2}";
        }

        return "00:00:00"; // Fallback value
    }

    // ConvertBack: String (mm:ss:ff) -> Long (Bytes)
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string timecode && !string.IsNullOrWhiteSpace(timecode))
        {
            try
            {
                string[] parts = timecode.Split(':');
                if (parts.Length == 3 &&
                    long.TryParse(parts[0], out long minutes) &&
                    long.TryParse(parts[1], out long seconds) &&
                    long.TryParse(parts[2], out long frames))
                {
                    long totalSectors = (minutes * 60 * FramesPerSecond) + (seconds * FramesPerSecond) + frames;
                    return totalSectors * BytesPerSector;
                }
            }
            catch
            {
                // Fail silently or log depending on requirements
            }
        }

        return 0L; // Fallback value
    }
}