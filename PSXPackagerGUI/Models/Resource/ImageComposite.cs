using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;

namespace PSXPackagerGUI.Models.Resource;

public class ImageComposite : BaseNotifyModel
{
    private BitmapSource? _alphaLayerMask;
    private bool _hasLayers;
    private Layer? _selectedLayer;

    private ObservableCollection<Layer> _layers;
    private ImageSource _compositeBitmap;
    private BitmapSource _selectionBitmap;

    public int Width { get; set; }
    public int Height { get; set; }

    public ObservableCollection<Layer> Layers
    {
        get => _layers;
        set => SetProperty(ref _layers, value);
    }

    public Layer? SelectedLayer
    {
        get => _selectedLayer;
        set => SetProperty(ref _selectedLayer, value);
    }

    public ImageSource CompositeBitmap
    {
        get => _compositeBitmap;
        set => SetProperty(ref _compositeBitmap, value);
    }

    public BitmapSource SelectionBitmap
    {
        get => _selectionBitmap;
        set => SetProperty(ref _selectionBitmap, value);
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
        RenderSelection();
    }

    public void RenderSelection()
    {
        var visual = new DrawingVisual();

        using (var dc = visual.RenderOpen())
        {
            var layer = SelectedLayer;

            if (layer != null)
            {
                var cornerBrush = new SolidColorBrush(Color.FromRgb(192, 192, 192));
                var pen = new Pen(cornerBrush, 1);

                dc.DrawRectangle(null, pen, new Rect(layer.OffsetX - 2, layer.OffsetY - 2, 4, 4));
                dc.DrawRectangle(null, pen, new Rect(layer.OffsetX + layer.Width - 2, layer.OffsetY - 2, 4, 4));
                dc.DrawRectangle(null, pen, new Rect(layer.OffsetX + layer.Width - 2, layer.OffsetY + layer.Height - 2, 4, 4));
                dc.DrawRectangle(null, pen, new Rect(layer.OffsetX - 2, layer.OffsetY + layer.Height - 2, 4, 4));
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

        SelectionBitmap = rtb;

    }


    public void RenderLayers()
    {
        if (Layers.Count == 0)
            return;

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