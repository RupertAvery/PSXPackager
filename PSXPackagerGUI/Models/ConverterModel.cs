using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
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
    private bool _isMergeEnabled;
    private bool _isMoveEnabled;

    public ConvertMode ConvertMode { get; set; }

    public ConverterModel()
    {
        _binPaths = new ObservableCollection<string>();
        PropertyChanged += OnPropertyChanged;
    }

    public void SetSuggestedPaths()
    {
        switch (ConvertMode)
        {
            case ConvertMode.BINS when BinPaths.Count > 0:
                TargetFileName = StripTrackName(Path.GetFileNameWithoutExtension(BinPaths[0]));
                TargetPath = Path.GetDirectoryName(BinPaths[0]);
                break;
            case ConvertMode.CUE when CueFile != null:
                TargetFileName = StripTrackName(Path.GetFileNameWithoutExtension(CueFile.FileEntries[0].FileName));
                TargetPath = Path.GetDirectoryName(CueFile.Path);
                break;
        }

        string StripTrackName(string filename)
        {
            var trackRegex = new Regex("\\(Track\\s*\\d+\\)|Track\\s*\\d+", RegexOptions.IgnoreCase);
            var sanitizedName = trackRegex.Replace(filename, "");

            return sanitizedName.Trim();
        }
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        IsMergeEnabled = BinPaths.Count > 1 &&
                         TargetFileName is { Length: > 0 } && TargetPath is { Length: > 0 };

        if (e.PropertyName == nameof(ConvertMode))
        {
            IsMoveEnabled = ConvertMode == ConvertMode.BINS;
        }

    }

    public bool IsMoveEnabled
    {
        get => _isMoveEnabled;
        set => SetProperty(ref _isMoveEnabled, value);
    }

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

    public bool IsMergeEnabled
    {
        get => _isMergeEnabled;
        set => SetProperty(ref _isMergeEnabled, value);
    }

    public CueFile? CueFile { get; set; }


}

