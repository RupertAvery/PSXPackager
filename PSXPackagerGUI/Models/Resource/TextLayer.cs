using System.Text.Json.Serialization;
using System.Windows.Markup;
using System.Windows.Media;

namespace PSXPackagerGUI.Models.Resource;

public class TextLayer : Layer
{
    public override LayerType LayerType => LayerType.Text;

    [JsonConverter(typeof(FontFamilyConverter))]
    public FontFamily FontFamily { get; set; }
    public string FontFamilyName { get; set; }

    public double FontSize { get; set; } = 12;
    public string TextContent { get; set; } = "Sample Text";

    [JsonConverter(typeof(BrushConverter))]
    public Brush Color { get; set; }
    public bool DropShadow { get; set; }

    private double _calculatedWidth;
    private double _calculatedHeight;

    public TextLayer()
    {

    }


    public TextLayer(string name, string text, FontFamily fontFamily, double fontSize, Brush color, bool dropShadow, int width, int height)
    {
        Name = name;
        TextContent = text;
        FontFamily = fontFamily;
        FontSize = fontSize;
        Color = color;
        DropShadow = dropShadow;
        OriginalWidth = width;
        OriginalHeight = height;

        RecalculateExtents();

        OffsetX = 0;
        OffsetY = 0;
        ScaleX = 1;
        ScaleY = 1;
        StrechMode = StretchMode.None;
    }

    public void RecalculateExtents()
    {
        var extents = CalculateExtents();

        _calculatedWidth = extents.Width + 5;
        _calculatedHeight = extents.Height;

        Width = (int)_calculatedWidth;
        Height = (int)_calculatedHeight;
    }

    private (double Width, double Height) CalculateExtents()
    {
        FormattedText formattedText = TextFormattingHelper.GetFormattedText(TextContent, FontFamily, FontSize);

        // Set a maximum width and height. If the text overflows these values, an ellipsis "..." appears.
        formattedText.MaxTextWidth = OriginalWidth;
        formattedText.MaxTextHeight = OriginalHeight;

        // Use a larger font size beginning at the first (zero-based) character and continuing for 5 characters.
        // The font size is calculated in terms of points -- not as device-independent pixels.

        // Use a Bold font weight beginning at the 6th character and continuing for 11 characters.
        // formattedText.SetFontWeight(FontWeights.Bold, 6, 11);
        return (formattedText.Width, formattedText.Height);
    }

    public override void Reset()
    {
        base.Reset();
        Width = (int)_calculatedWidth;
        Height = (int)_calculatedHeight;
    }
}