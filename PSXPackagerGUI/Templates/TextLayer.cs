using System.Xml.Serialization;

namespace PSXPackagerGUI.Templates;

public class TextLayer : Layer
{
    public string Text { get; set; }
    public string FontFamily { get; set; }
    public double FontSize { get; set; }
    public string Color { get; set; }
    public bool DropShadow { get; set; }
}