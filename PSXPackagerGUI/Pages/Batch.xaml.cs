using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Popstation.M3u;
using PSXPackager.Common.Cue;
using Path = System.IO.Path;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Batch.xaml
    /// </summary>
    public partial class Batch : Page
    {
        private BatchModel _model;
        private BatchController _controller;

        public Batch()
        {
            InitializeComponent();
            _model = new BatchModel();
            _controller = new BatchController(_model, this, Dispatcher);
            DataContext = _model;
        }


    }

    public class BatchController
    {
        private readonly BatchModel _model;
        private readonly Page _page;
        private readonly Dispatcher _dispatcher;

        private Window Window => Window.GetWindow(_page);

        public BatchController(BatchModel model, Page page, Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _model = model;
            _page = page;

            _model.ConvertImageToPbp = true;
            _model.IsBinChecked = true;
            _model.IsImgChecked = true;
            _model.IsM3uChecked = true;
            _model.IsIsoChecked = true;

            _model.BatchEntries = new ObservableCollection<BatchEntryModel>();


            _model.ScanCommand = new RelayCommand(Scan);
            _model.ProcessCommand = new RelayCommand(ProcessFiles);
            _model.BrowseInputCommand = new RelayCommand(BrowseInput);
            _model.BrowseOutputCommand = new RelayCommand(BrowseOutput);
        }

        private void Scan(object obj)
        {
            if (string.IsNullOrEmpty(_model.InputPath))
            {
                return;
            }

            if (!Directory.Exists(_model.InputPath))
            {
                MessageBox.Show(Window, "Invalid directory or directory not found", "Batch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var patterns = new List<string>();
            if (_model.IsM3uChecked)
            {
                patterns.Add("*.m3u");
            }
            if (_model.IsBinChecked)
            {
                patterns.Add("*.cue");
                patterns.Add("*.bin");
            }
            if (_model.IsImgChecked)
            {
                patterns.Add("*.img");
            }
            if (_model.IsIsoChecked)
            {
                patterns.Add("*.iso");
            }

            Task.Run(() =>
            {
                _dispatcher.Invoke(() =>
                {
                    _model.BatchEntries.Clear();
                });

                var ignoreFileSet = new HashSet<string>();

                string GetFullPath(string file)
                {
                    return Path.Combine(_model.InputPath, file);
                }

                foreach (var pattern in patterns)
                {
                    var files = Directory.EnumerateFiles(_model.InputPath, pattern, SearchOption.TopDirectoryOnly);

                    foreach (var file in files.Select(GetFullPath))
                    {
                        if (Path.GetFileName(file) == "Lunar - Silver Star Story Complete (USA) (Disc 1).cue")
                        {
                            var x = 1;
                        }
                        if (Path.GetFileName(file) == "Lunar - Silver Star Story Complete (USA) (Disc 1) (Track 1).bin")
                        {
                            var x = 1;
                        }
                        if (pattern == "*.m3u")
                        {
                            var playlist = M3uFileReader.Read(file);
                            foreach (var fileEntry in playlist.FileEntries)
                            {
                                ignoreFileSet.Add(GetFullPath(fileEntry));
                            }
                        }
                        if (pattern == "*.cue")
                        {
                            var cueFiles = CueFileReader.Read(file);
                            foreach (var fileEntry in cueFiles.FileEntries)
                            {
                                ignoreFileSet.Add(GetFullPath(fileEntry.FileName));
                            }
                        }

                        if (!ignoreFileSet.Contains(file))
                        {
                            _dispatcher.Invoke(() =>
                            {
                                _model.BatchEntries.Add(new BatchEntryModel()
                                {
                                    Path = file,
                                });
                            });
                        }
                    }
                }
            });
        }

        private void ProcessFiles(object obj)
        {
            if (string.IsNullOrEmpty(_model.OutputPath))
            {
                return;
            }

            if (!Directory.Exists(_model.OutputPath))
            {
                MessageBox.Show(Window, "Invalid directory or directory not found", "Batch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void BrowseInput(object obj)
        {
            var folderBrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            folderBrowserDialog.ShowDialog(Window);
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                return;
            }

            _model.InputPath = folderBrowserDialog.SelectedPath;
        }

        private void BrowseOutput(object obj)
        {
            var folderBrowserDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            folderBrowserDialog.ShowDialog(Window);
            if (string.IsNullOrEmpty(folderBrowserDialog.SelectedPath))
            {
                return;
            }

            _model.OutputPath = folderBrowserDialog.SelectedPath;

        }
    }

    public class Processor
    {
        //private readonly Channel<ProcessOption> _channel = Channel.CreateUnbounded<int>();

        public void AddJob()
        {

        }

        public void ProcessJobs()
        {
            while (true)
            {
                //_channel.Reader.TryRead(out var )
            }
        }
    }

    public class BatchEntryModel : BaseNotifyModel
    {
        private string _path;
        private double _maxProgress;
        private double _progress;
        private string _status;

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }

        public double MaxProgress
        {
            get => _maxProgress;
            set => SetProperty(ref _maxProgress, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }

    public class BatchModel : BaseNotifyModel
    {
        private ObservableCollection<BatchEntryModel> _batchEntries;
        private string _inputPath;
        private string _outputPath;
        private ICommand _browseInputCommand;
        private ICommand _browseOutputCommand;
        private ICommand _scanCommand;
        private ICommand _processCommand;
        private bool _isScanning;
        private bool _isProcessing;
        private bool _isBinChecked;
        private bool _isM3UChecked;
        private bool _isIsoChecked;
        private bool _isImgChecked;
        private string _status;
        private double _progress;
        private double _maxProgress;
        private bool _convertImageToPbp;
        private bool _convertPbpToImage;

        public ObservableCollection<BatchEntryModel> BatchEntries
        {
            get => _batchEntries;
            set => SetProperty(ref _batchEntries, value);
        }

        public string InputPath
        {
            get => _inputPath;
            set => SetProperty(ref _inputPath, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => SetProperty(ref _outputPath, value);
        }

        public ICommand BrowseInputCommand
        {
            get => _browseInputCommand;
            set => SetProperty(ref _browseInputCommand, value);
        }

        public ICommand BrowseOutputCommand
        {
            get => _browseOutputCommand;
            set => SetProperty(ref _browseOutputCommand, value);
        }

        public ICommand ScanCommand
        {
            get => _scanCommand;
            set => SetProperty(ref _scanCommand, value);
        }

        public ICommand ProcessCommand
        {
            get => _processCommand;
            set => SetProperty(ref _processCommand, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public bool IsBinChecked
        {
            get => _isBinChecked;
            set => SetProperty(ref _isBinChecked, value);
        }

        public bool IsM3uChecked
        {
            get => _isM3UChecked;
            set => _isM3UChecked = value;
        }

        public bool IsIsoChecked
        {
            get => _isIsoChecked;
            set => SetProperty(ref _isIsoChecked, value);
        }

        public bool IsImgChecked
        {
            get => _isImgChecked;
            set => SetProperty(ref _isImgChecked, value);
        }


        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public double Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public double MaxProgress
        {
            get => _maxProgress;
            set => SetProperty(ref _maxProgress, value);
        }

        public bool ConvertImageToPbp
        {
            get => _convertImageToPbp;
            set => SetProperty(ref _convertImageToPbp, value);
        }

        public bool ConvertPbpToImage
        {
            get => _convertPbpToImage;
            set => SetProperty(ref _convertPbpToImage, value);
        }
    }
}
