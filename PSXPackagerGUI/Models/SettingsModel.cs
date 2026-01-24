using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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
        private bool _generateIconFrame;
        private string? _lastDiscImageDirectory;
        private string? _lastResourceDirectory;
        private string? _lastTemplateDirectory;
        private ConverterModel _converter;

        public SettingsModel()
        {
            _converter = new ConverterModel
            {
                BinPaths = new ObservableCollection<string>()
            };
        }

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

        public bool GenerateIconFrame
        {
            get => _generateIconFrame;
            set => SetProperty(ref _generateIconFrame, value);
        }

        public string? LastDiscImageDirectory
        {
            get => _lastDiscImageDirectory;
            set => SetProperty(ref _lastDiscImageDirectory, value);
        }

        public string? LastResourceDirectory
        {
            get => _lastResourceDirectory;
            set => SetProperty(ref _lastResourceDirectory, value);
        }

        public string? LastTemplateDirectory
        {
            get => _lastTemplateDirectory;
            set => SetProperty(ref _lastTemplateDirectory, value);
        }


        [JsonIgnore]
        public ConverterModel Converter
        {
            get => _converter;
            set => SetProperty(ref _converter, value);
        }
    }
}