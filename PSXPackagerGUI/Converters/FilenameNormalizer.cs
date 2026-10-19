using System.Text.RegularExpressions;

namespace PSXPackagerGUI.Converters;

public static class FilenameNormalizer
{
    private static readonly Regex pbpRegex = new Regex(@"^pbp://(.*?)/disc(\d+)$");

    public static string Normalize(string filename)
    {
        var match = pbpRegex.Match(filename);
        if (match.Success)
        {
            filename = match.Groups[1].Value;
            var disc = int.Parse(match.Groups[2].Value) + 1;
            return $"{filename} (Slot {disc})";
        }
        return filename;
    }
}