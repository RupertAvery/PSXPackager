using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace PSXPackagerGUI.Models.Resource;

public class ImageComposite : BaseNotifyModel
{
    private BitmapSource? _alphaLayerMask;
    private bool _hasLayers;

    private ObservableCollection<Layer> _layers;
    private ImageSource _compositeBitmap;

    public int Width { get; set; }
    public int Height { get; set; }

    public ObservableCollection<Layer> Layers
    {
        get => _layers;
        set => SetProperty(ref _layers, value);
    }

    public ImageSource CompositeBitmap
    {
        get => _compositeBitmap;
        set => SetProperty(ref _compositeBitmap, value);
    }

    public bool HasLayers
    {
        get => _hasLayers;
        set => SetProperty(ref _hasLayers, value);
    }

    public bool IsDirty { get; private set; }

    public void SetPristine()
    {
        IsDirty = false;
    }

    public ImageComposite(int width, int height)
    {
        Width = width;
        Height = height;
        Layers = new ObservableCollection<Layer>();
        Layers.CollectionChanged += LayersOnCollectionChanged;
        Render();
    }

    private void LayersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasLayers = Layers.Count > 0;
    }

    public void SetAplhaMask(BitmapSource bitmapSource)
    {
        _alphaLayerMask = bitmapSource;
    }

    public void RemoveAplhaMask()
    {
        _alphaLayerMask = null;
    }

    public void Render()
    {
        RenderLayers();
    }


    public void RenderLayers()
    {
        var visual = new DrawingVisual();


        using (var dc = visual.RenderOpen())
        {
            if (_alphaLayerMask != null)
            {
                // IMPORTANT: Push mask BEFORE drawing content
                dc.PushOpacityMask(new ImageBrush(_alphaLayerMask));
            }

            foreach (var layer in Layers)
            {
                if (layer is ImageLayer img)
                {
                    dc.DrawImage(
                        img.Bitmap,
                        new Rect(
                            img.OffsetX,
                            img.OffsetY,
                            img.Width,
                            img.Height));
                }
                else if (layer is TextLayer text)
                {
                    var typeFace = new Typeface(text.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                    var formattedText = new FormattedText(
                            text.TextContent,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            typeFace,
                            text.FontSize, 
                            text.Color,
                            96
                        );
                    formattedText.MaxTextWidth = text.Width;

                    var point = new Point(text.OffsetX, text.OffsetY);
                    
                    dc.DrawText(formattedText, point);

                    if (text.DropShadow)
                    {
                        var shadowText = new FormattedText(
                            text.TextContent,
                            CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            typeFace,
                            text.FontSize, 
                            new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)),
                            96
                        );
                        shadowText.MaxTextWidth = text.Width;
                        dc.DrawText(shadowText, new Point(point.X + 2, point.Y + 2));
                    }
                }
            }
        }

        var rtb = new RenderTargetBitmap(
        (int)Width,
        (int)Height,
        96,
        96,
        PixelFormats.Pbgra32);

        rtb.Render(visual);
        rtb.Freeze();

        CompositeBitmap = rtb;

        IsDirty = true;
    }

    public void Clear()
    {
        Layers.Clear();
    }

    public void SaveToPNG(Stream stream)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create((BitmapSource)CompositeBitmap));
        encoder.Save(stream);
    }

    public void InsertLayerAfter(Layer layer, Layer targetLayer)
    {
        Layers.Insert(Layers.IndexOf(targetLayer) + 1, layer);
    }

    public void AddLayer(Layer layer)
    {
        Layers.Add(layer);
    }

    public void MoveLayerDown(Layer layer)
    {
        var index = Layers.IndexOf(layer);
        if (index < Layers.Count - 1)
        {
            Layers.Move(index, index + 1);
        }
    }

    public void MoveLayerUp(Layer layer)
    {
        var index = Layers.IndexOf(layer);
        if (index > 0)
        {
            Layers.Move(index, index - 1);
        }
    }
}