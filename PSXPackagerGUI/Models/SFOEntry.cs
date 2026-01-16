namespace PSXPackagerGUI.Models;

public class SFOEntry : BaseNotifyModel
{
    private string _key;
    private object _value;
    private bool _isEditable;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public object Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public bool IsEditable
    {
        get => _isEditable;
        set => SetProperty(ref _isEditable, value);
    }

    public SFOEntryType EntryType { get; set; }
}