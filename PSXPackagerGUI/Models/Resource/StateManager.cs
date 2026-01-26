using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;

namespace PSXPackagerGUI.Models.Resource;

public class StateManager
{
    private readonly ImageComposite _imageComposite;
    private BitmapSourceCache _bitmapSourceCache = new BitmapSourceCache();
    private Stack<string> _undoStack = new();
    private Stack<string> _redoStack = new();

    private string? _savedState;

    
    public StateManager(ImageComposite imageComposite)
    {
        _imageComposite = imageComposite;
    }

    public void SaveState()
    {
        _savedState = GetCurrentState();
    }

    public void ClearState()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _bitmapSourceCache.Clear();
    }

    public void CommitState()
    {
        if (_savedState != null)
        {
            _undoStack.Push(_savedState);
            _redoStack.Clear();
        }
    }
    public void PushState()
    {
        var state = GetCurrentState();
        _undoStack.Push(state);
    }

    private void PushCurrentUndoState()
    {
        var state = GetCurrentState();
        _undoStack.Push(state);
    }

    private void PushCurrentRedoState()
    {
        var state = GetCurrentState();
        _redoStack.Push(state);
    }

    public void UndoState()
    {
        if (_undoStack.Count > 0)
        {
            PushCurrentRedoState();
            var state = _undoStack.Pop();
            RestoreState(state);
        }
    }

    public void RedoState()
    {
        if (_redoStack.Count > 0)
        {
            PushCurrentUndoState();
            var state = _redoStack.Pop();
            RestoreState(state);
        }
    }

    private void RestoreState(string state)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new FontFamilyConverter(), new BrushConverter() }
        };

        _imageComposite.Layers = new ObservableCollection<Layer>(JsonSerializer.Deserialize<List<Layer>>(state, options));

        foreach (var layer in _imageComposite.Layers)
        {
            if (layer.Hash != null && _bitmapSourceCache.TryGet(layer.Hash, out var bitmap))
            {
                layer.Bitmap = bitmap;
            }
        }
    }
    
    private string GetCurrentState()
    {
        var layers = _imageComposite.Layers.ToList();

        foreach (var layer in layers)
        {
            if (layer.Hash != null && !_bitmapSourceCache.Contains(layer.Hash))
            {
                _bitmapSourceCache.Add(layer.Hash, layer.Bitmap);
            }
        }

        var options = new JsonSerializerOptions
        {
            Converters = { new FontFamilyConverter(), new BrushConverter() }
        };

        return JsonSerializer.Serialize(layers, options);
    }

}