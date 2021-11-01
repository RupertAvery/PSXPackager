using System.Collections.ObjectModel;
using System.Windows.Input;

namespace PSXPackagerGUI.Pages
{
    public class BatchModel : BaseNotifyModel
    {
        private ObservableCollection<BatchEntryModel> _batchEntries;
        private string _inputPath;
        private string _outputPath;
        private ICommand _browseInputCommand;
        private ICommand _browseOutputCommand;
        private ICommand _scanCommand;
        private ICommand _processCommand;
        private bool _isScanning;
        private bool _isProcessing;
        private bool _isBinChecked;
        private bool _isM3UChecked;
        private bool _isIsoChecked;
        private bool _isImgChecked;
        private string _status;
        private double _progress;
        private double _maxProgress;
        private bool _convertImageToPbp;
        private bool _convertPbpToImage;
        private int _compressionLevel;
        private string _fileNameFormat;

        public ObservableCollection<BatchEntryModel> BatchEntries
        {
            get => _batchEntries;
            set => SetProperty(ref _batchEntries, value);
        }

        public string InputPath
        {
            get => _inputPath;
            set => SetProperty(ref _inputPath, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
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
            set => SetProperty(ref _isScanning, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public bool IsBinChecked
        {
            get => _isBinChecked;
            set => SetProperty(ref _isBinChecked, value);
        }

        public bool IsM3uChecked
        {
            get => _isM3UChecked;
            set => _isM3UChecked = value;
        }

        public bool IsIsoChecked
        {
            get => _isIsoChecked;
            set => SetProperty(ref _isIsoChecked, value);
        }

        public bool IsImgChecked
        {
            get => _isImgChecked;
            set => SetProperty(ref _isImgChecked, value);
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

        public bool IsBusy { get; set; }
    }
}