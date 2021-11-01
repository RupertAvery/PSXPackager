using System.ComponentModel;
using System.Windows.Controls;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private readonly Configuration<SettingsModel> _configuration;
        public SettingsModel Model { get; set; }
        public SettingsPage()
        {
            InitializeComponent();

            _configuration = new Configuration<SettingsModel>();
            
            if (!_configuration.TryLoad(out var settings))
            {
                settings = new SettingsModel
                {
                    CompressionLevel = 5,
                    FileNameFormat = "%FILENAME%",
                };
                _configuration.Save(settings);
            }

            Model = settings;

            Model.SourceFilename = "Final Fantasy 7 (Disc 2).bin";
            // Force update SampleFilename
            ModelOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(SettingsModel.FileNameFormat)));

            DataContext = Model;

            Model.PropertyChanged += ModelOnPropertyChanged;
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsModel.FileNameFormat):
                    var sample = "SCUS-94164;SCUS94163;Final Fantasy VII;Final Fantasy VII - Disc 2;NTSC;SCUS94164";
                    var splitSample = sample.Split(new[] { ';' });
                    var mainGameId = splitSample[1];
                    var maintitle = splitSample[2];
                    var title = splitSample[3];
                    var region = splitSample[4];
                    var gameId = splitSample[5];

                    Model.SampleFilename = Popstation.Popstation.GetFilename(Model.FileNameFormat, Model.SourceFilename, gameId, mainGameId, title, maintitle, region) + ".pbp";

                    break;
                case nameof(SettingsModel.SampleFilename):
                    return;
            }

            _configuration.Save(Model);
        }
    }
}
