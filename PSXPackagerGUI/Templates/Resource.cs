using Popstation.Pbp;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PSXPackagerGUI.Templates
{
    public class Resource
    {
        public ResourceType ResourceType { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Name { get; set; }

        [XmlElement(typeof(ImageLayer))]
        [XmlElement(typeof(TextLayer))]
        public List<Layer> Layers { get; set; } = new List<Layer>();
    }
}
