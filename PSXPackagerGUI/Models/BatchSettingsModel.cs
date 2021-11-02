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


    }
}