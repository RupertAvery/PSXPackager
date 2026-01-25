using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Popstation.Pbp;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class SettingsPage : Page
    {
        private readonly Window _window;
        private readonly Configuration<SettingsModel> _configuration;
        public SettingsModel Model { get; set; }

        public bool IsFirstRun { get; }

        public SettingsPage(Window window)
        {
            _window = window;
            InitializeComponent();

            _configuration = new Configuration<SettingsModel>("PSXPackagerUI");

            if (!_configuration.TryLoad(out var settings))
            {
                IsFirstRun = true;
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

            ServiceLocator.Settings = Model;

            var version = typeof(SettingsPage).Assembly.GetName().Version;

            Model.Version = $"PSXPackagerGUI {version.Major}.{version.Minor}.{version.Build}";

            if (string.IsNullOrEmpty(settings.FileNameFormat))
            {
                settings.FileNameFormat = "%GAMEID%\\EBOOT";
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

        private Window Window => _window;

        private void BrowseCustomResourcePath(object obj)
        {
            var folderBrowserDialog = new Microsoft.Win32.OpenFolderDialog();
            var result = folderBrowserDialog.ShowDialog(Window);
            if (result is true)
            {
                Model.CustomResourcesPath = folderBrowserDialog.FolderName;
            }
        }

        const string sample = "SCUS-94164;SCUS94163;Final Fantasy VII;Final Fantasy VII - Disc 2;NTSC;SCUS94164";

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var splitSample = sample.Split(new[] { ';' });
            var mainGameId = splitSample[1];
            var maintitle = splitSample[2];
            var title = splitSample[3];
            var region = splitSample[4];
            var gameId = splitSample[5];

            switch (e.PropertyName)
            {
                case nameof(SettingsModel.FileNameFormat):
                    Model.SampleFilename = Popstation.Popstation.GetFilename(Model.FileNameFormat,
                        Model.SourceFilename,
                        gameId,
                        mainGameId,
                        title,
                        maintitle,
                        region) + ".pbp";
                    break;

                case nameof(SettingsModel.CustomResourcesFormat):
                    Model.SampleResourcePath = Popstation.Popstation.GetResourceFilename(Model.CustomResourcesFormat,
                        Model.SourceFilename,
                        gameId,
                        mainGameId,
                        title,
                        maintitle,
                        region,
                        ResourceType.ICON0,
                        "png");

                    break;
                case nameof(SettingsModel.SampleFilename):
                    return;
            }

            _configuration.Save(Model);
        }

        private void PSPDefault_OnClick(object sender, RoutedEventArgs e)
        {
            Model.FileNameFormat = "%GAMEID%\\EBOOT";
        }

        private void EmulatorDefault_OnClick(object sender, RoutedEventArgs e)
        {
            Model.FileNameFormat = "%FILENAME%";
        }

        private void OpenSettingsFolderButton_OnClick(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                Arguments = Path.GetDirectoryName(_configuration.SettingsPath),
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }

       
    }
}
