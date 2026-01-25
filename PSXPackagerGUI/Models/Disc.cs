using System.Collections.Generic;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PSXPackagerGUI.Models
{
    public enum TrackStatus
    {
        Stopped,
        Playing,
    }

    public class Track : INotifyPropertyChanged 
    {
        private bool _isSelected;
        private TrackStatus _status;
        private int _number;
        private string _dataType;

        public TrackStatus Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public int Number
        {
            get => _number;
            set => SetField(ref _number, value);
        }

        public string DataType
        {
            get => _dataType;
            set => SetField(ref _dataType, value);
        }

        public CueTrack CueTrack { get; private set; }

        public Track()
        {
        }
        
        public Track(CueTrack track)
        {
            DataType = track.DataType;
            Number = track.Number;
            CueTrack = track;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class Disc : BaseNotifyModel
    {
        private string _title;
        private string _gameId;
        private string _region;
        private uint _size;
        private bool _isEmpty;
        private bool _isRemoveEnabled;
        private bool _isSaveAsEnabled;
        private bool _isLoadEnabled;
        private ICommand _removeCommand;
        private ObservableCollection<Track> _tracks;

        public int Index { get; set; }

        public Disc()
        {
            Index = -1;
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ICommand RemoveCommand
        {
            get => _removeCommand;
            set => SetProperty(ref _removeCommand, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        public bool IsLoadEnabled
        {
            get => _isLoadEnabled;
            set => SetProperty(ref _isLoadEnabled, value);
        }

        public bool IsSaveAsEnabled
        {
            get => _isSaveAsEnabled;
            set => SetProperty(ref _isSaveAsEnabled, value);
        }

        public bool IsRemoveEnabled
        {
            get => _isRemoveEnabled;
            set => SetProperty(ref _isRemoveEnabled, value);
        }

        public uint Size
        {
            get => _size;
            set => SetProperty(ref _size, value);
        }

        public string GameID
        {
            get => _gameId;
            set => SetProperty(ref _gameId, value);
        }

        public string Region
        {
            get => _region;
            set => SetProperty(ref _region, value);
        }


        public string? SourceUrl { get; set; }
        public string? SourceTOC { get; set; }

        public ObservableCollection<Track> Tracks
        {
            get => _tracks;
            set => SetProperty(ref _tracks, value);
        }

        public static Disc EmptyDisc(int index)
        {
            return new Disc()
            {
                Index = index,
                Title = "No Disc",
                IsEmpty = true,
                IsRemoveEnabled = false,
                IsLoadEnabled = true,
                IsSaveAsEnabled = false,
            };
        }
    }

}