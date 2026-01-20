using System.Xml.Serialization;

namespace PSXPackagerGUI.Templates;


public abstract class Layer
{
    [XmlAttribute]
    public double X { get; set; }
    [XmlAttribute]
    public double Y { get; set; }
    [XmlAttribute]
    public double Width { get; set; }
    [XmlAttribute]
    public double Height { get; set; }
    [XmlAttribute]
    public double OriginalWidth { get; set; }
    [XmlAttribute]
    public double OriginalHeight { get; set; }
}