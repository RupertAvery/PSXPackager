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
        public MainWindow()
        {

            InitializeComponent();
            _single = new Single();

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

        }

    }

}
