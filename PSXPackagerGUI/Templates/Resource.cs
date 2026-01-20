using Popstation.Pbp;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PSXPackagerGUI.Templates
{
    public class Resource
    {
        [XmlAttribute]
        public ResourceType ResourceType { get; set; }
        [XmlAttribute]
        public int Width { get; set; }
        [XmlAttribute]
        public int Height { get; set; }
        [XmlAttribute]
        public string Name { get; set; }

        [XmlElement(typeof(ImageLayer))]
        [XmlElement(typeof(TextLayer))]
        public List<Layer> Layers { get; set; } = new List<Layer>();
    }
}
