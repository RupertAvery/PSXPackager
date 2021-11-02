using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PSXPackagerGUI.Models
{
    public class BatchModel : BaseNotifyModel
    {
        private ObservableCollection<BatchEntryModel> _batchEntries;
        private ICommand _browseInputCommand;
        private ICommand _browseOutputCommand;
        private ICommand _scanCommand;
        private ICommand _processCommand;
        private bool _isScanning;
        private bool _isProcessing;
        private string _status;
        private double _progress;
        private double _maxProgress;

        private bool _convertImageToPbp;
        private bool _convertPbpToImage;
        private bool _generateResourceFolders;
        private bool _extractResources;

        public ObservableCollection<BatchEntryModel> BatchEntries
        {
            get => _batchEntries;
            set => SetProperty(ref _batchEntries, value);
        }

        public ICommand BrowseInputCommand
        {
            get => _browseInputCommand;
            set => SetProperty(ref _browseInputCommand, value);
        }

        public ICommand BrowseOutputCommand
        {
            get => _browseOutputCommand;
            set => SetProperty(ref _browseOutputCommand, value);
        }

        public ICommand ScanCommand
        {
            get => _scanCommand;
            set => SetProperty(ref _scanCommand, value);
        }

        public ICommand ProcessCommand
        {
            get => _processCommand;
            set => SetProperty(ref _processCommand, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                 SetProperty(ref _isScanning, value);
                 OnPropertyChanged(nameof(IsBusy));
            }
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                SetProperty(ref _isProcessing, value);
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public double MaxProgress
        {
            get => _maxProgress;
            set => SetProperty(ref _maxProgress, value);
        }

        public bool ConvertImageToPbp
        {
            get => _convertImageToPbp;
            set => SetProperty(ref _convertImageToPbp, value);
        }

        public bool ConvertPbpToImage
        {
            get => _convertPbpToImage;
            set => SetProperty(ref _convertPbpToImage, value);
        }


        public bool GenerateResourceFolders
        {
            get => _generateResourceFolders;
            set => SetProperty(ref _generateResourceFolders, value);
        }

        public bool ExtractResources
        {
            get => _extractResources;
            set => SetProperty(ref _extractResources, value);
        }

        public BatchSettingsModel Settings { get; set; }

        public bool IsBusy => IsProcessing || IsScanning;

    }
}