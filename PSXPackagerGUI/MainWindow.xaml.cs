using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using Popstation.Database;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;
using PSXPackagerGUI.Pages;

namespace PSXPackagerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SinglePage _singlePage;
        private readonly BatchPage _batchPage;
        private readonly SettingsPage _settings;
        private readonly MainModel _model;
        private readonly GameDB _gameDb = new GameDB(Path.Combine(ApplicationInfo.AppPath, "Resources", "gameInfo.db"));

        private bool _isFirstRun;

        public MainWindow()
        {
            var dllPath = Path.Combine(System.Environment.Is64BitOperatingSystem ? "x64" : "x86", "7z.dll");

            var sevenZipLibPath = Path.Combine(ApplicationInfo.AppPath, dllPath);

            if (!File.Exists(sevenZipLibPath))
            {
                MessageBox.Show($"{dllPath} not found!", "PSXPackager GUI", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            InitializeComponent();

            _settings = new SettingsPage(this);
            _isFirstRun = _settings.IsFirstRun;


            _singlePage = new SinglePage(this, _settings.Model, _gameDb);
            _batchPage = new BatchPage(this, _settings.Model, _gameDb);
            _singlePage.Model.PropertyChanged += ModelOnPropertyChanged;

            _model = new MainModel();
            
            DataContext = _model;
            
            _model.Mode = AppMode.Single;
            
            CurrentPage.Content = _singlePage;
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SingleModel.IsDirty))
            {
                _model.IsDirty = _singlePage.Model.IsDirty;
            }
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _singlePage.OnClosing(e);
            _batchPage.OnClosing(e);
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            _singlePage.LoadPbp();
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _singlePage.Save();
        }
        
        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentPage.Content = _settings;
        }

        private void SingleMode_OnClick(object sender, RoutedEventArgs e)
        {
            _model.Mode = AppMode.Single;
            CurrentPage.Content = _singlePage;
        }

        private void BatchMode_OnClick(object sender, RoutedEventArgs e)
        {
            _model.Mode = AppMode.Batch;
            CurrentPage.Content = _batchPage;
        }

        private void CreateFile_OnClick(object sender, RoutedEventArgs e)
        {
            _singlePage.NewPBP();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isFirstRun)
            {
                var message = "Do you want to use PSP Settings for batch processing? " +
                              "A folder named using the Game ID will be generated and the EBOOT.PBP file will be placed there. \r\n\r\n" +
                              "e.g.: SLUSXXXXX\\EBOOT.PBP\r\n\r\n" +
                              "You can change this at anytime the File Format tab of the Settings page";
                var result = MessageBox.Show(this, message, "PSXPackager", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _settings.Model.FileNameFormat = "%GAMEID%\\EBOOT";
                }
                else
                {
                    _settings.Model.FileNameFormat = "%FILENAME%";
                }
            }
        }

        private void SavePSP_OnClick(object sender, RoutedEventArgs e)
        {
            _singlePage.SavePSP();
        }
    }
}
