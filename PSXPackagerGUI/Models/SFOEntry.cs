using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace PSXPackagerGUI.Models;

public class SFOEntry : BaseNotifyModel
{
    private string _key;
    private object _value;
    private bool _isEditable;
    private int _maxLength;
    private bool _isValid;
    private string _toolTip;

    public SFOEntry()
    {
        PropertyChanged += OnPropertyChanged;
        IsValid = true;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Validator != null && Value != null)
        {
            IsValid = Validator(Value.ToString());
        }
    }

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

    public int MaxLength
    {
        get => _maxLength;
        set => SetProperty(ref _maxLength, value);
    }

    public SFOEntryType EntryType { get; set; }
 
    public Func<string, bool>? Validator { get; set; }

    public bool IsValid
    {
        get => _isValid;
        set => SetProperty(ref _isValid, value);
    }

    public string ToolTip
    {
        get => _toolTip;
        set => SetProperty(ref _toolTip, value);
    }
}