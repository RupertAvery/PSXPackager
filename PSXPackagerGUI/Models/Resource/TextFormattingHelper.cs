using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PSXPackagerGUI.Models.Resource;

public static class TextFormattingHelper
{
    public static Visual Visual = null!; 
    
    public static double GetPixelsPerDip()
    {
        return VisualTreeHelper.GetDpi(Visual).PixelsPerDip;
    }

    public static FormattedText GetFormattedText(string text, FontFamily fontFamily, double fontSize)
    {
        return new FormattedText(
            text,
            CultureInfo.GetCultureInfo("en-us"),
            FlowDirection.LeftToRight,
            new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
            fontSize,
            Brushes.Black, new NumberSubstitution(), TextFormattingMode.Ideal, GetPixelsPerDip());
    }
}