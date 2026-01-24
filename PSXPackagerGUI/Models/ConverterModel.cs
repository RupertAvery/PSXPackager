using System.Collections.ObjectModel;
using System.Windows;
using PSXPackager.Common.Cue;

namespace PSXPackagerGUI.Models;

public enum ConvertMode
{
    CUE,
    BINS
}

public class ConverterModel : BaseNotifyModel
{
    private string _basePath;
    private ObservableCollection<string> _binPaths;
    private string _targetPath;
    private string _targetFileName;
    private int _selectedIndex;

    public ConvertMode ConvertMode { get; set; }

    public string BasePath
    {
        get => _basePath;
        set => SetProperty(ref _basePath, value);
    }

    public ObservableCollection<string> BinPaths
    {
        get => _binPaths;
        set => SetProperty(ref _binPaths, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, value);
    }

    public string TargetPath
    {
        get => _targetPath;
        set => SetProperty(ref _targetPath, value);
    }

    public string TargetFileName
    {
        get => _targetFileName;
        set => SetProperty(ref _targetFileName, value);
    }

    public CueFile? CueFile { get; set; }
}

