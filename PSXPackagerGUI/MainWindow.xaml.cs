using System.Windows;
using PSXPackagerGUI.Pages;

namespace PSXPackagerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Single _single;
        private Batch _batch;
        private Settings _settings;
        private MainModel _model;
        public MainWindow()
        {

            InitializeComponent();
            _single = new Single();
            _batch = new Batch();
            _settings = new Settings();
            _model = new MainModel();
            DataContext = _model;
            _model.Mode = AppMode.Single;
            CurrentPage.Content = _single;
        }



        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            openFileDialog.Filter = "PBP Files|*.pbp|All files|*.*";
            openFileDialog.ShowDialog();
            if (!string.IsNullOrEmpty(openFileDialog.FileName))
            {
                _single.LoadPbp(openFileDialog.FileName);
            }
        }

        private void NewPBP_OnClick(object sender, RoutedEventArgs e)
        {
            this.Save.IsEnabled = !this.Save.IsEnabled;
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            _single.Save();
        }
        
        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentPage.Content = _settings;
        }

        private void Batch_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void SingleMode_OnClick(object sender, RoutedEventArgs e)
        {
            _model.Mode = AppMode.Single;
            CurrentPage.Content = _single;
        }

        private void BatchMode_OnClick(object sender, RoutedEventArgs e)
        {
            _model.Mode = AppMode.Batch;
            CurrentPage.Content = _batch;
        }
    }
}
