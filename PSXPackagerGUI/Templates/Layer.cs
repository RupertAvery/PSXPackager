using System.Xml.Serialization;

namespace PSXPackagerGUI.Templates;


public abstract class Layer
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double OriginalWidth { get; set; }
    public double OriginalHeight { get; set; }
}