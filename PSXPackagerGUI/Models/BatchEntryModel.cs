using System.Collections.Generic;
using System.IO;

namespace PSXPackagerGUI.Models
{
    public enum ScanEntryType
    {
        File,
        CueSheet,
        PlayList
    }

    public enum SubEntryType
    {
        File,
        CueSheet,
        PlayList
    }

    public abstract class SubEntry
    {
        public abstract SubEntryType SubEntryType { get; }
        public string Path { get; set; }
        public string RelativePath { get; set; }
    }

    public class FileEntry : SubEntry
    {
        public override SubEntryType SubEntryType => SubEntryType.File;
    }

    public class CueFileEntry : SubEntry
    {
        public override SubEntryType SubEntryType => SubEntryType.CueSheet;
        public IEnumerable<FileEntry> FileEntries { get; set; }
    }

    public class PlaylistEntry : SubEntry
    {
        public override SubEntryType SubEntryType => SubEntryType.PlayList;
        public IEnumerable<FileEntry> FileEntries { get; set; }
    }


    public class BatchEntryModel : BaseNotifyModel
    {
        private string _relativePath;
        private double _maxProgress;
        private double _progress;
        private string _status;
        private string _errorMessage;
        private bool _hasError;
        private bool _isSelected;
        private string _mainGameId;
        private string _gameId;
        private List<SubEntry> _subEntries;
        private bool _isExpanded;

        public string RelativePath
        {
            get => _relativePath;
            set => SetProperty(ref _relativePath, value);
        }

        public double MaxProgress
        {
            get => _maxProgress;
            set => SetProperty(ref _maxProgress, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public string MainGameId
        {
            get => _mainGameId;
            set => SetProperty(ref _mainGameId, value);
        }

        public string GameId
        {
            get => _gameId;
            set => SetProperty(ref _gameId, value);
        }

        public List<SubEntry> SubEntries
        {
            get => _subEntries;
            set => SetProperty(ref _subEntries, value);
        }

        public bool HasSubEntries => SubEntries is { Count: > 0 };

        public ScanEntryType Type { get; set; }
    }
}