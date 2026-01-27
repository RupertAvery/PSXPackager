using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using static System.Net.Mime.MediaTypeNames;

namespace PSXPackagerGUI.Models.Resource;

public class ImageLayer : Layer
{
    public override LayerType LayerType => LayerType.Image;
    public string SourceUri { get; set; }

    public ImageLayer()
    {

    }

    public ImageLayer(BitmapSource bitmap, string name, string sourceUri)
    {
        SourceUri = sourceUri;
        Name = name;
        Bitmap = bitmap;
        Width = bitmap.PixelWidth;
        Height = bitmap.PixelHeight;
        OriginalWidth = Width;
        OriginalHeight = Height;
        OffsetX = 0;
        OffsetY = 0;
        ScaleX = 1;
        ScaleY = 1;
        StrechMode = StretchMode.None;
        SetPristine();
    }


}