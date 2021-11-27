using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Popstation.Database;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Batch.xaml
    /// </summary>
    public partial class BatchPage : Page
    {
        private readonly Window _window;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly BatchModel _model;
        private BatchController _controller;

        public BatchPage(Window window, SettingsModel settings, GameDB gameDb)
        {
            _window = window;
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeComponent();

            _model = new BatchModel()
            {
                Settings = settings.Batch
            };

            _controller = new BatchController(_model, settings, _window, Dispatcher, gameDb, _cancellationTokenSource.Token);
            _controller.Cancel = () =>
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                _controller.UpdateToken(_cancellationTokenSource.Token);
            };
            DataContext = _model;
        }


        public bool IsBusy => _model.IsBusy;

        public Window Window => _window;

        public void OnClosing(CancelEventArgs e)
        {
            if (IsBusy)
            {
                var result = MessageBox.Show(Window, "An operation is in progress. Are you sure you want to cancel?", "PSXPackager",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.Yes);
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource.Dispose();
        }
    }
}
