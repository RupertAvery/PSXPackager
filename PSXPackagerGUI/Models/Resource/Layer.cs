using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Models.Resource;

public abstract class Layer : BaseNotifyModel
{
    private BitmapSource _bitmap;
    private string _name;

    protected double OriginalWidth;
    protected double OriginalHeight;
    private double _offsetX;
    private double _offsetY;
    private double _scaleX;
    private double _scaleY;
    private double _width;
    private double _height;


    public abstract LayerType LayerType { get; }


    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public BitmapSource Bitmap
    {
        get => _bitmap;
        set => SetProperty(ref _bitmap, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value);
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value);
    }

    public double OffsetX
    {
        get => _offsetX;
        set => SetProperty(ref _offsetX, value);
    }

    public double OffsetY
    {
        get => _offsetY;
        set => SetProperty(ref _offsetY, value);
    }

    public double ScaleX
    {
        get => _scaleX;
        set => SetProperty(ref _scaleX, value);
    }

    public double ScaleY
    {
        get => _scaleY;
        set => SetProperty(ref _scaleY, value);
    }

    public StretchMode StrechMode { get; set; }

    public virtual void Reset()
    {
        OffsetX = 0;
        OffsetY = 0;
        Width = OriginalWidth;
        Height = OriginalHeight;
        ScaleX = 1;
        ScaleY = 1;
        StrechMode = StretchMode.None;
    }
}