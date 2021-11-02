using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Popstation.Pbp;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;

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

            _configuration = new Configuration<SettingsModel>("PSXPackagerUI");

            if (!_configuration.TryLoad(out var settings))
            {
                settings = new SettingsModel
                {
                    CompressionLevel = 5,
                    Batch = new BatchSettingsModel()
                    {
                        IsBinChecked = true,
                        IsImgChecked = true,
                        IsM3uChecked = true,
                        IsIsoChecked = true,
                    }
                };
                _configuration.Save(settings);
            }

            Model = settings;

            if (string.IsNullOrEmpty(settings.FileNameFormat))
            {
                settings.FileNameFormat = "%FILENAME%";
            }

            if (string.IsNullOrEmpty(settings.CustomResourcesFormat))
            {
                settings.CustomResourcesFormat = "%FILENAME%\\%RESOURCE%.%EXT%";
            }

            if (settings.Batch == null)
            {
                settings.Batch = new BatchSettingsModel()
                {
                    IsBinChecked = true,
                    IsImgChecked = true,
                    IsM3uChecked = true,
                    IsIsoChecked = true,
                };
            }

            Model.SourceFilename = "Final Fantasy 7 (Disc 2).bin";
            Model.BrowseCustomResourcePath = new RelayCommand(BrowseCustomResourcePath);

            // Force update SampleFilename
            ModelOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(SettingsModel.FileNameFormat)));
            ModelOnPropertyChanged(this, new PropertyChangedEventArgs(nameof(SettingsModel.CustomResourcesFormat)));

            DataContext = Model;

            Model.PropertyChanged += ModelOnPropertyChanged;
            Model.Batch.PropertyChanged += ModelOnPropertyChanged;


        }

        private Window Window => Window.GetWindow(this);

        private void BrowseCustomResourcePath(object obj)
        {
            var folderBrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            folderBrowserDialog.ShowDialog(Window);
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                return;
            }

            Model.CustomResourcesPath = folderBrowserDialog.SelectedPath;
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SettingsModel.FileNameFormat):
                    {
                        var sample = "SCUS-94164;SCUS94163;Final Fantasy VII;Final Fantasy VII - Disc 2;NTSC;SCUS94164";
                        var splitSample = sample.Split(new[] { ';' });
                        var mainGameId = splitSample[1];
                        var maintitle = splitSample[2];
                        var title = splitSample[3];
                        var region = splitSample[4];
                        var gameId = splitSample[5];

                        Model.SampleFilename = Popstation.Popstation.GetFilename(Model.FileNameFormat, Model.SourceFilename,
                            gameId, mainGameId, title, maintitle, region) + ".pbp";
                    }
                    break;

                case nameof(SettingsModel.CustomResourcesFormat):
                    {
                        var sample = "SCUS-94164;SCUS94163;Final Fantasy VII;Final Fantasy VII - Disc 2;NTSC;SCUS94164";
                        var splitSample = sample.Split(new[] { ';' });
                        var mainGameId = splitSample[1];
                        var maintitle = splitSample[2];
                        var title = splitSample[3];
                        var region = splitSample[4];
                        var gameId = splitSample[5];

                        Model.SampleResourcePath = Popstation.Popstation.GetResourceFilename(Model.CustomResourcesFormat, Model.SourceFilename, gameId, mainGameId, title, maintitle, region, ResourceType.ICON0, "png");
                    }

                    break;
                case nameof(SettingsModel.SampleFilename):
                    return;
            }

            _configuration.Save(Model);
        }
    }
}
