﻿using System;
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
using Ookii.Dialogs.Wpf;
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
        private CancellationToken _token;

        private Window Window => Window.GetWindow(_page);

        public BatchController(BatchModel model, SettingsModel settings, Page page, Dispatcher dispatcher, GameDB gameDb,
            CancellationToken token)
        {
            _dispatcher = dispatcher;
            _gameDb = gameDb;
            _token = token;
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
            _model.ProcessCommand = new RelayCommand((o) => ProcessFiles(_token));
            _model.BrowseInputCommand = new RelayCommand(BrowseInput);
            _model.BrowseOutputCommand = new RelayCommand(BrowseOutput);
        }

        public Action Cancel { get; set; }

        private void Scan(object obj)
        {
            if (_model.IsScanning)
            {
                var result = MessageBox.Show(Window, "Abort scanning?", "Batch", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    Cancel();
                }
                return;
            }

            if (string.IsNullOrEmpty(_model.InputPath))
            {

                MessageBox.Show(Window, "No input path specified", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    _model.IsScanning = true;
                    _model.BatchEntries.Clear();
                });

                var ignoreFileSet = new HashSet<string>();

                string GetFullPath(string file)
                {
                    return Path.Combine(_model.InputPath, file);
                }

                foreach (var pattern in patterns)
                {
                    if (_token.IsCancellationRequested) break;

                    var files = Directory.EnumerateFiles(_model.InputPath, pattern, SearchOption.TopDirectoryOnly);

                    foreach (var file in files.Select(GetFullPath))
                    {
                        if (_token.IsCancellationRequested) break;

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

                        var relativePath = Path.GetRelativePath(_model.InputPath, file);

                        if (!ignoreFileSet.Contains(file))
                        {
                            _dispatcher.Invoke(() =>
                            {
                                _model.BatchEntries.Add(new BatchEntryModel()
                                {
                                    RelativePath = relativePath,
                                    MaxProgress = 100,
                                    Progress = 0,
                                    Status = "Ready"
                                });
                            });
                        }
                    }
                }

                _dispatcher.Invoke(() =>
                {
                    _model.IsScanning = false;
                });

                if (_token.IsCancellationRequested)
                {
                    _dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Window, "Scan aborted!", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }

            });
        }

        private void ProcessFiles(CancellationToken token)
        {
            if (_model.IsProcessing)
            {
                var result = MessageBox.Show(Window, "Abort processing?", "Batch", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    Cancel();
                }
                return;
            }

            if (string.IsNullOrEmpty(_model.OutputPath))
            {
                MessageBox.Show(Window, "No output path specified", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(_model.OutputPath))
            {
                MessageBox.Show(Window, "Invalid directory or directory not found", "Batch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_model.BatchEntries.Count == 0)
            {
                MessageBox.Show(Window, "Nothing to process. Please Scan a directory first.", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var processor = new Processor(_dispatcher, _gameDb, _settings, new ProcessEventHandler());

            foreach (var entry in _model.BatchEntries.Where(e => e.Status != "Complete"))
            {
                entry.HasError = false;
                entry.MaxProgress = 100;
                entry.Progress = 0;
                entry.ErrorMessage = null;
                entry.Status = "Queued";

                processor.Add(new ConvertJob()
                {
                    Entry = entry
                });
            }

            _dispatcher.Invoke(() =>
            {
                _model.IsProcessing = true;
            });

            processor.Start(_model, token).ContinueWith(t =>
            {
                _dispatcher.Invoke(() =>
                {
                    _model.IsProcessing = false;
                });

                if (t.IsCompletedSuccessfully)
                {
                    if (token.IsCancellationRequested)
                    {
                        _dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Window, "Conversion aborted!", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
                        });
                    }
                    else
                    {
                        _dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Window, "Conversion completed.", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Information);
                        });
                    }

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

        public void UpdateToken(CancellationToken token)
        {
            _token = token;
        }
    }
}

