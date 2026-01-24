using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace PSXPackagerGUI.Models.Resource;

public static class FontManager
{
    public static FontFamily NewRodinProDBFontFamily { get; private set; }

    public static Typeface NewRodinProDB { get; private set; }

    static FontManager()
    {
        //pack://application:,,,/Resources/Editor/#FOT-NewRodin Pro DB

        var fontUri = new Uri(
            "pack://application:,,,/Resources/Editor/#FOT-NewRodin Pro DB",
            UriKind.Absolute);

        // EXACT family name from font properties
        NewRodinProDBFontFamily =
            new FontFamily(fontUri, "NewRodinPro-DB");

        NewRodinProDB = new Typeface(
            NewRodinProDBFontFamily,
            FontStyles.Normal,
            FontWeights.Normal,
            FontStretches.Normal);

    }

    public static FontFamily GetFontFamily(string familyName)
    {
        return familyName == "NewRodin Pro DB" ? NewRodinProDBFontFamily : new FontFamily(familyName);
    }
}