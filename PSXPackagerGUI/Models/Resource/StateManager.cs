using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;

namespace PSXPackagerGUI.Models.Resource;

public class StateChangedEventArgs
{

}


public delegate void StateChangedEventHandler(
    object? sender,
    StateChangedEventArgs e);

public class StateManager
{
    private readonly ImageComposite _imageComposite;
    private readonly BitmapSourceCache _bitmapSourceCache = new BitmapSourceCache();
    private readonly Stack<string> _undoStack = new();
    private readonly Stack<string> _redoStack = new();

    private string? _savedState;

    public event StateChangedEventHandler? StateChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

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
        OnStateChanged(new StateChangedEventArgs());
    }

    public void CommitState()
    {
        if (_savedState != null)
        {
            _undoStack.Push(_savedState);
            _redoStack.Clear();
            OnStateChanged(new StateChangedEventArgs());
        }
    }
    public void PushState()
    {
        var state = GetCurrentState();
        _undoStack.Push(state);
        OnStateChanged(new StateChangedEventArgs());
    }

    private void PushCurrentUndoState()
    {
        var state = GetCurrentState();
        _undoStack.Push(state);
        OnStateChanged(new StateChangedEventArgs());
    }

    private void PushCurrentRedoState()
    {
        var state = GetCurrentState();
        _redoStack.Push(state);
        OnStateChanged(new StateChangedEventArgs());
    }

    public void UndoState()
    {
        if (_undoStack.Count > 0)
        {
            PushCurrentRedoState();
            var state = _undoStack.Pop();
            RestoreState(state);
            OnStateChanged(new StateChangedEventArgs());
        }
    }

    public void RedoState()
    {
        if (_redoStack.Count > 0)
        {
            PushCurrentUndoState();
            var state = _redoStack.Pop();
            RestoreState(state);
            OnStateChanged(new StateChangedEventArgs());
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

    protected virtual void OnStateChanged(StateChangedEventArgs e)
    {
        StateChanged?.Invoke(this, e);
    }
}