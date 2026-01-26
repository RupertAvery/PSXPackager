using System.Windows;
using PSXPackagerGUI.Models.Resource;

namespace PSXPackagerGUI.Controls;

public class HoverEventArgs : RoutedEventArgs
{
    public Layer Layer { get; }
    public bool IsSelected { get; }
    public HoverEventArgs(RoutedEvent routedEvent, object source, Layer layer, bool isSelected) : base(routedEvent, source)
    {
        Layer = layer;
        IsSelected = isSelected;
    }
}