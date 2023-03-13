namespace PSXPackagerGUI.Models
{
    public class BatchEntryModel : BaseNotifyModel
    {
        private string _relativePath;
        private double _maxProgress;
        private double _progress;
        private string _status;
        private string _errorMessage;
        private bool _hasError;
        private bool _isSelected;

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

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}