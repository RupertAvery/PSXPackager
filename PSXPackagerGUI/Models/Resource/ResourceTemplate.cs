using System.Collections.Generic;
using Popstation.Pbp;

namespace PSXPackagerGUI.Models.Resource;

public class ResourceTemplate {
    public ResourceType ResourceType { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<Layer> Layers { get; set; }
}