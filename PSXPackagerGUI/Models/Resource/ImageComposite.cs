using System.Collections.Generic;
using PSXPackagerGUI.Templates;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
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
    private ObservableCollection<Layer> _reverseLayers;
    private ImageSource _compositeBitmap;
    private StateManager _stateManager;
    private bool _canRedo;
    private bool _canUndo;

    public int Width { get; set; }
    public int Height { get; set; }

    public bool CanUndo
    {
        get => _stateManager.CanUndo;
    }

    public bool CanRedo
    {
        get => _stateManager.CanRedo;
    }

    private void LayersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasLayers = Layers.Count > 0;

        foreach (var layer in Layers)
        {
            layer.CanMoveUp = Layers.IndexOf(layer) < Layers.Count - 1;
            layer.CanMoveDown = Layers.IndexOf(layer) > 0;
        }

        OnPropertyChanged(nameof(ReverseLayers));
    }

    private void ReverseLayersOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {

    }

    public ImageComposite(int width, int height)
    {
        Width = width;
        Height = height;
        Layers = new ObservableCollection<Layer>();
        _stateManager = new StateManager(this);
        _stateManager.StateChanged += StateManagerOnStateChanged;
        Render();
    }

    private void StateManagerOnStateChanged(object? sender, StateChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
    }


    public ObservableCollection<Layer> Layers
    {
        get => _layers;
        set
        {
            SetProperty(ref _layers, value);
            if (_layers != null)
                _layers.CollectionChanged -= LayersOnCollectionChanged;

            _layers = value;

            if (_layers != null)
            {
                _layers.CollectionChanged += LayersOnCollectionChanged;

                HasLayers = _layers.Count > 0;

                foreach (var layer in _layers)
                {
                    layer.CanMoveUp = _layers.IndexOf(layer) < Layers.Count - 1;
                    layer.CanMoveDown = _layers.IndexOf(layer) > 0;
                }

                OnPropertyChanged(nameof(ReverseLayers));
            }

        }
    }

    public IEnumerable<Layer> ReverseLayers
    {
        get => _layers.Reverse();
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
        _stateManager.ClearState();
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
        if (index > 0)
        {
            Layers.Move(index, index - 1);
        }
    }

    public void MoveLayerUp(Layer layer)
    {
        var index = Layers.IndexOf(layer);
        if (index < Layers.Count - 1)
        {
            Layers.Move(index, index + 1);
        }
    }

    public void PushState()
    {
        _stateManager.PushState();
    }

    public void SaveState()
    {
        _stateManager.SaveState();
    }

    public void UndoState()
    {
        _stateManager.UndoState();
    }

    public void RedoState()
    {
        _stateManager.RedoState();
    }

    public void CommitState()
    {
        _stateManager.CommitState();
    }
}