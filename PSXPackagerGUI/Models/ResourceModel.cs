using System.Windows.Media;
using Popstation.Pbp;

namespace PSXPackagerGUI.Models
{
    public class ResourceModel : BaseNotifyModel
    {
        private ImageSource _icon;
        private bool _isRemoveEnabled;
        private bool _isSaveAsEnabled;
        private bool _isLoadEnabled;
        private bool _text;
        private bool _isMoreEnabled;

        public ResourceModel()
        {
            IsLoadEnabled = true;
        }

        public void Reset()
        {
            Icon = null;
            IsLoadEnabled = true;
            IsSaveAsEnabled = false;
            IsRemoveEnabled = false;
            SourceUrl = null;
        }

        public ResourceType Type { get; set; }

        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
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

        public bool IsMoreEnabled
        {
            get => _isMoreEnabled;
            set => SetProperty(ref _isMoreEnabled, value);
        }

        public bool IsRemoveEnabled
        {
            get => _isRemoveEnabled;
            set => SetProperty(ref _isRemoveEnabled, value);
        }

        public bool Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public string SourceUrl { get; set; }
    }
}