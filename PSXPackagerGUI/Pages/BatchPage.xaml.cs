using System.IO;
using System.Threading;
using System.Windows.Controls;
using Popstation.Database;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Batch.xaml
    /// </summary>
    public partial class BatchPage : Page
    {
        private readonly BatchModel _model;
        private BatchController _controller;

        public BatchPage(SettingsModel settings, GameDB gameDb, CancellationTokenSource cancellationTokenSource)
        {
            InitializeComponent();
            _model = new BatchModel();
            _controller = new BatchController(_model, settings, this, Dispatcher, gameDb, cancellationTokenSource);
            DataContext = _model;
        }


        public bool IsBusy => _model.IsBusy;

    }
}
