using PSXPackagerGUI.Models.Resource;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Models
{
    public class SingleModel : BaseNotifyModel
    {
        private ObservableCollection<Disc> _discs;
        private bool _isDirty;
        private double _progress;
        private double _maxProgress;
        private Disc _selectedDisc;
        private ResourceModel _icon0;
        private ResourceModel _icon1;
        private ResourceModel _icon1Pmf;
        private ResourceModel _pic0;
        private ResourceModel _pic1;
        private ResourceModel _snd0;
        private ResourceModel _boot;
        private string _status;
        private ObservableCollection<SFOEntry> _sfoEntries;
        private SettingsModel _settings;
        private bool _showIcon;
        private bool _showInformation;
        private bool _showBackground;
        private Track? _selectedTrack;
        private string _currentTime;
        private ResourceModel _currentResource;
        private string _currentResourceName;
        private string _saveId;
        private string _saveTitle;

        public SettingsModel Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public SingleModel()
        {
            ShowBackground = true;
            ShowInformation = true;
            ShowIcon = true;
        }

        public Disc SelectedDisc { get => _selectedDisc; set => SetProperty(ref _selectedDisc, value); }
        public ObservableCollection<Disc> Discs { get => _discs; set => SetProperty(ref _discs, value); }

        public ObservableCollection<SFOEntry> SFOEntries
        {
            get => _sfoEntries;
            set => SetProperty(ref _sfoEntries, value);
        }

        public ResourceModel CurrentResource
        {
            get => _currentResource;
            set => SetProperty(ref _currentResource, value);
        }

        public ResourceModel Icon0 { get => _icon0; set => SetProperty(ref _icon0, value); }
        public ResourceModel Icon1 { get => _icon1; set => SetProperty(ref _icon1, value); }
        public ResourceModel Pic0 { get => _pic0; set => SetProperty(ref _pic0, value); }
        public ResourceModel Pic1 { get => _pic1; set => SetProperty(ref _pic1, value); }
        public ResourceModel Snd0 { get => _snd0; set => SetProperty(ref _snd0, value); }
        public ResourceModel Boot { get => _boot; set => SetProperty(ref _boot, value); }


        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public double MaxProgress { get => _maxProgress; set => SetProperty(ref _maxProgress, value); }


        public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }
        public bool IsBusy { get; set; }

        public bool IsNew { get; set; }

        public bool ShowBackground
        {
            get => _showBackground;
            set => SetProperty(ref _showBackground, value);
        }

        public bool ShowInformation
        {
            get => _showInformation;
            set => SetProperty(ref _showInformation, value);
        }

        public bool ShowIcon
        {
            get => _showIcon;
            set => SetProperty(ref _showIcon, value);
        }

        public Track? SelectedTrack
        {
            get => _selectedTrack;
            set => SetProperty(ref _selectedTrack, value);
        }

        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        public string CurrentResourceName
        {
            get => _currentResourceName;
            set => SetProperty(ref _currentResourceName, value);
        }

        public string SaveID
        {
            get => _saveId;
            set => SetProperty(ref _saveId, value);
        }

        public string SaveTitle
        {
            get => _saveTitle;
            set => SetProperty(ref _saveTitle, value);
        }
    }
}