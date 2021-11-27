namespace PSXPackagerGUI.Models
{
    public class BatchSettingsModel : BaseNotifyModel
    {
        private string _inputPath;
        private string _outputPath;
        private bool _isBinChecked;
        private bool _isM3UChecked;
        private bool _isIsoChecked;
        private bool _isImgChecked;
        private bool _is7zChecked;
        private bool _isZipChecked;
        private bool _isRarChecked;
        private bool _recurseFolders;

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

        public bool IsBinChecked
        {
            get => _isBinChecked;
            set => SetProperty(ref _isBinChecked, value);
        }

        public bool IsM3uChecked
        {
            get => _isM3UChecked;
            set => SetProperty(ref _isM3UChecked, value);
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

        public bool Is7zChecked
        {
            get => _is7zChecked;
            set => SetProperty(ref _is7zChecked, value);
        }

        public bool IsZipChecked
        {
            get => _isZipChecked;
            set => SetProperty(ref _isZipChecked, value);
        }

        public bool IsRarChecked
        {
            get => _isRarChecked;
            set => SetProperty(ref _isRarChecked, value);
        }

        public bool RecurseFolders
        {
            get => _recurseFolders;
            set => SetProperty(ref _recurseFolders, value);
        }
    }
}