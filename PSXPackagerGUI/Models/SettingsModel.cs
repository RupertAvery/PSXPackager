using System.Windows.Input;
using Newtonsoft.Json;

namespace PSXPackagerGUI.Models
{
    public class SettingsModel : BaseNotifyModel
    {
        private string _fileNameFormat;
        private string _sampleFilename;
        private string _sampleResourcePath;
        private int _compressionLevel;
        private bool _useCustomResources;
        private string _customResourcesFormat;
        private string _customResourcesPath;
        private ICommand _browseCustomResourcePath;

        public string FileNameFormat
        {
            get => _fileNameFormat;
            set => SetProperty(ref _fileNameFormat, value);
        }

        public int CompressionLevel
        {
            get => _compressionLevel;
            set => SetProperty(ref _compressionLevel, value);
        }

        public bool UseCustomResources
        {
            get => _useCustomResources;
            set => SetProperty(ref _useCustomResources, value);
        }

        public string CustomResourcesFormat
        {
            get => _customResourcesFormat;
            set => SetProperty(ref _customResourcesFormat, value);
        }

        public string CustomResourcesPath
        {
            get => _customResourcesPath;
            set => SetProperty(ref _customResourcesPath, value);
        }

        [JsonIgnore]
        public ICommand BrowseCustomResourcePath
        {
            get => _browseCustomResourcePath;
            set => SetProperty(ref _browseCustomResourcePath, value);
        }

        public BatchSettingsModel Batch { get; set; }


        [JsonIgnore]
        public string SampleResourcePath
        {
            get => _sampleResourcePath;
            set => SetProperty(ref _sampleResourcePath, value);
        }

        [JsonIgnore]
        public string SampleFilename
        {
            get => _sampleFilename;
            set => SetProperty(ref _sampleFilename, value);
        }

        [JsonIgnore]
        public string SourceFilename { get; set; }

        [JsonIgnore]
        public string Version { get; set; }
    }
}