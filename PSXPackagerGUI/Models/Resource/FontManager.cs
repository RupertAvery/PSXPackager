using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PSXPackagerGUI.Models.Resource;

public static class FontManager
{
    private static string ResourcePath = Path.Combine(ApplicationInfo.AppPath, "Resources");

    public static FontFamily NewRodinProDBFontFamily { get; private set; }

    public static Typeface NewRodinProDB { get; private set; }

    static FontManager()
    {
        var fontUri = new Uri(Path.Combine(ResourcePath, "Editor", "NewRodin Pro DB.otf"), UriKind.Absolute);

        NewRodinProDBFontFamily = new FontFamily(fontUri, "NewRodin Pro DB");

        NewRodinProDB = new Typeface(
            NewRodinProDBFontFamily,
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

    }
}