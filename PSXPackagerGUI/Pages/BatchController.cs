using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Popstation.Database;
using Popstation.M3u;
using PSXPackager.Common.Cue;

namespace PSXPackagerGUI.Pages
{
    public class BatchController
    {
        private readonly BatchModel _model;
        private readonly SettingsModel _settings;
        private readonly Page _page;
        private readonly Dispatcher _dispatcher;
        private readonly GameDB _gameDb;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private Window Window => Window.GetWindow(_page);

        public BatchController(BatchModel model, SettingsModel settings, Page page, Dispatcher dispatcher, GameDB gameDb,
            CancellationTokenSource cancellationTokenSource)
        {
            _dispatcher = dispatcher;
            _gameDb = gameDb;
            _cancellationTokenSource = cancellationTokenSource;
            _model = model;
            _settings = settings;
            _page = page;

            _model.MaxProgress = 100;
            _model.Progress = 0;

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
                                    MaxProgress = 100,
                                    Progress = 0,
                                    Status = "Queued"
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

            var processor = new Processor(_dispatcher, _gameDb, _settings, new ProcessEventHandler());

            foreach (var entry in _model.BatchEntries)
            {
                entry.HasError = false;
                entry.MaxProgress = 100;
                entry.Progress = 0;
                entry.ErrorMessage = "";

                processor.Add(new ConvertJob()
                {
                    Entry = entry
                });
            }

            processor.Start(_model, _cancellationTokenSource.Token).ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                {
                    _dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Window, "Conversion completed.", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            });
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
}

