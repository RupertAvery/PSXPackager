using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Controls;

public class Point : BaseNotifyModel
{
    private double _x;
    private double _y;

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }
}