using Newtonsoft.Json;

namespace PSXPackagerGUI.Pages
{
    public class SettingsModel : BaseNotifyModel
    {
        private string _fileNameFormat;
        private string _sampleFilename;
        private int _compressionLevel;
        private bool _useCustomResources;
        private string _customResourcesFormat;
        private string _customResourcesPath;

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
        public string SampleFilename
        {
            get => _sampleFilename;
            set => SetProperty(ref _sampleFilename, value);
        }

        [JsonIgnore]
        public string SourceFilename { get; set; }
    }
}