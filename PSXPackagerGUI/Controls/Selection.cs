using System.Windows;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Controls;

public class Selection : BaseNotifyModel
{
    private Point _c1;
    private Point _c2;
    private Point _c3;
    private Point _c4;
    private Point _e4;
    private Point _e3;
    private Point _e2;
    private Point _e1;
    private Visibility _visibility;

    public Selection()
    {
        C1= new Point();
        C2 = new Point();
        C3 = new Point();
        C4 = new Point();
        E1 = new Point();
        E2 = new Point();
        E3 = new Point();
        E4 = new Point();
        Visibility = Visibility.Hidden;
    }

    public Point E4
    {
        get => _e4;
        set => SetProperty(ref _e4, value);
    }

    public Point E3
    {
        get => _e3;
        set => SetProperty(ref _e3, value);
    }

    public Point E2
    {
        get => _e2;
        set => SetProperty(ref _e2, value);
    }

    public Point E1
    {
        get => _e1;
        set => SetProperty(ref _e1, value);
    }

    public Point C1
    {
        get => _c1;
        set => SetProperty(ref _c1, value);
    }

    public Point C2
    {
        get => _c2;
        set => SetProperty(ref _c2, value);
    }

    public Point C3
    {
        get => _c3;
        set => SetProperty(ref _c3, value);
    }

    public Point C4
    {
        get => _c4;
        set => SetProperty(ref _c4, value);
    }

    public Visibility Visibility
    {
        get => _visibility;
        set => SetProperty(ref _visibility, value);
    }
}