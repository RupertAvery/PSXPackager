using System.Windows;
using PSXPackagerGUI.Models.Resource;

namespace PSXPackagerGUI.Controls;

public class SelectLayerEventArgs : RoutedEventArgs
{
    public Layer Layer { get; }
    public SelectLayerEventArgs(RoutedEvent routedEvent, object source, Layer layer) : base(routedEvent, source)
    {
        Layer = layer;
    }
}