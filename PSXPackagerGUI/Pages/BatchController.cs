using DiscUtils;
using Popstation.Database;
using Popstation.M3u;
using PSXPackager.Common.Cue;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;
using PSXPackagerGUI.Processing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CueFileEntry = PSXPackagerGUI.Models.CueFileEntry;

namespace PSXPackagerGUI.Pages
{
    public class BatchController
    {
        private readonly BatchModel _model;
        private readonly SettingsModel _settings;
        private readonly Dispatcher _dispatcher;
        private readonly GameDB _gameDb;
        private CancellationToken _token;
        private Window _window;
        private Window Window => _window;

        public BatchController(BatchModel model, SettingsModel settings, Window window, Dispatcher dispatcher, GameDB gameDb,
            CancellationToken token)
        {
            _dispatcher = dispatcher;
            _gameDb = gameDb;
            _token = token;
            _model = model;
            _settings = settings;
            _window = window;

            _model.MaxProgress = 100;
            _model.Progress = 0;

            _model.ConvertImageToPbp = true;


            _model.BatchEntries = new ObservableCollection<BatchEntryModel>();
            _model.BatchEntries.CollectionChanged += BatchEntriesOnCollectionChanged;

            _model.ScanCommand = new RelayCommand(Scan);
            _model.ProcessCommand = new RelayCommand((o) => ProcessFiles(_token));
            _model.BrowseInputCommand = new RelayCommand(BrowseInput);
            _model.BrowseOutputCommand = new RelayCommand(BrowseOutput);
        }

        private void BatchEntriesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (BatchEntryModel item in e.NewItems)
                {
                    item.PropertyChanged += ItemOnPropertyChanged;
                }
            }
        }

        private void ItemOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BatchEntryModel.IsSelected))
            {
                if (_model.BatchEntries.All(e => e.IsSelected))
                {
                    _model.SelectAll = true;
                }
                else if (_model.BatchEntries.All(e => !e.IsSelected))
                {
                    _model.SelectAll = false;
                }
                else
                {
                    _model.SelectAll = null;
                }

            }
        }

        public Action Cancel { get; set; }

        public static string GetLowestCommonFolder(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return string.Empty;

            // Normalize paths
            var separatedPaths = paths
                .Select(p => Path.GetFullPath(p)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .ToArray();

            var minLength = separatedPaths.Min(p => p.Length);
            var commonParts = new System.Collections.Generic.List<string>();

            for (int i = 0; i < minLength; i++)
            {
                var currentPart = separatedPaths[0][i];

                if (separatedPaths.All(p =>
                        string.Equals(p[i], currentPart,
                            OperatingSystem.IsWindows()
                                ? StringComparison.OrdinalIgnoreCase
                                : StringComparison.Ordinal)))
                {
                    commonParts.Add(currentPart);
                }
                else
                {
                    break;
                }
            }

            if (commonParts.Count == 0)
                return string.Empty;

            return string.Join(Path.DirectorySeparatorChar, commonParts);
        }

        private FileEntry GetCueFileEntry(string path, string basePath)
        {
            var extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".bin":
                    return new FileEntry()
                    {
                        Path = path,
                        RelativePath = Path.GetRelativePath(basePath, path),
                    };
            }

            throw new Exception("Unsupported file type");
        }

        private SubEntry GetPlaylistEntry(string path, string basePath)
        {
            var extension = Path.GetExtension(path).ToLower();

            switch (extension)
            {
                case ".cue":
                    var cueFiles = CueFileReader.Read(path);

                    var fileEntries = new List<FileEntry>();

                    foreach (var fileEntry in cueFiles.FileEntries)
                    {
                        fileEntries.Add(GetCueFileEntry(cueFiles.GetAbsolutePath(fileEntry), Path.GetDirectoryName(path)));
                    }

                    return new CueFileEntry()
                    {
                        Path = path,
                        RelativePath = Path.GetRelativePath(basePath, path),
                        FileEntries = fileEntries
                    };

                case ".bin":
                    return new FileEntry()
                    {
                        Path = path,
                        RelativePath = Path.GetRelativePath(basePath, path),
                    };
                case ".chd":
                    return new FileEntry()
                    {
                        Path = path,
                        RelativePath = Path.GetRelativePath(basePath, path),
                    };
            }

            throw new Exception("Unsupported file type");
        }

        private string GetDiscPath(string path)
        {
            var entryExtension = Path.GetExtension(path).ToLower();

            switch (entryExtension)
            {
                case ".m3u":
                    {
                        var playlist = M3uFileReader.Read(path);
                        return GetDiscPath(playlist.GetAbsolutePath(playlist.FileEntries[0]));
                    }
                case ".cue":
                    {
                        var cueFiles = CueFileReader.Read(path);
                        return GetDiscPath(cueFiles.GetAbsolutePath(cueFiles.FileEntries[0]));
                    }
                default:
                    return path;
            }
        }

        private IEnumerable<string> GetIgnoreFiles(SubEntry subEntry)
        {
            switch (subEntry.SubEntryType)
            {
                case SubEntryType.PlayList:
                    {
                        var playlist = subEntry as PlaylistEntry;

                        foreach (var fileEntry in playlist.FileEntries)
                        {
                            foreach (var entry in GetIgnoreFiles(fileEntry))
                            {
                                yield return entry;
                            }
                        }

                        break;
                    }
                case SubEntryType.CueSheet:
                    {
                        var cueFileEntry = subEntry as CueFileEntry;

                        foreach (var fileEntry in cueFileEntry.FileEntries)
                        {
                            foreach (var entry in GetIgnoreFiles(fileEntry))
                            {
                                yield return entry;
                            }
                        }

                        break;
                    }
                case SubEntryType.File:
                    yield return subEntry.Path;
                    break;
            }
        }

        private IEnumerable<string> GetIgnoreFiles(ScanEntry scanEntry)
        {
            switch (scanEntry.Type)
            {
                case ScanEntryType.PlayList:
                case ScanEntryType.CueSheet:
                    {
                        foreach (var subEntry in scanEntry.SubEntries)
                        {
                            yield return subEntry.Path;

                            foreach (var entry in GetIgnoreFiles(subEntry))
                            {
                                yield return entry;
                            }
                        }

                        break;
                    }
            }

            //if (scanEntry.Type == ScanEntryType.File)
            //{
            //    yield break;
            //}
        }

        private ScanEntry GetScanEntry(string path)
        {
            var entryExtension = Path.GetExtension(path).ToLower();

            var scanEntry = new ScanEntry()
            {
                Path = path
            };

            var basePath = Path.GetDirectoryName(path);

            switch (entryExtension)
            {
                case ".m3u":
                    {
                        scanEntry.Type = ScanEntryType.PlayList;

                        var playlist = M3uFileReader.Read(path);
                        var subEntries = new List<SubEntry>();

                        foreach (var fileEntry in playlist.FileEntries)
                        {
                            var absolutePath = playlist.GetAbsolutePath(fileEntry);
                            subEntries.Add(GetPlaylistEntry(absolutePath, basePath));
                        }

                        scanEntry.SubEntries = subEntries;

                        break;
                    }
                case ".cue":
                    {
                        scanEntry.Type = ScanEntryType.CueSheet;

                        var cueFiles = CueFileReader.Read(path);
                        var subEntries = new List<SubEntry>();

                        foreach (var fileEntry in cueFiles.FileEntries)
                        {
                            var absolutePath = cueFiles.GetAbsolutePath(fileEntry);
                            subEntries.Add(GetCueFileEntry(absolutePath, basePath));
                        }

                        scanEntry.SubEntries = subEntries;

                        break;
                    }
                default:
                    {
                        scanEntry.Type = ScanEntryType.File;
                        break;
                    }

            }

            return scanEntry;
        }

        private void Scan(object obj)
        {
            var gamesByMainGameId = _gameDb.GameEntries.ToLookup(d => d.GameID);

            if (_model.IsScanning)
            {
                var result = MessageBox.Show(Window, "Abort scanning?", "Batch", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    Cancel();
                }
                return;
            }

            if (string.IsNullOrEmpty(_model.Settings.InputPath))
            {

                MessageBox.Show(Window, "No input path specified", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(_model.Settings.InputPath))
            {
                MessageBox.Show(Window, "Invalid directory or directory not found", "Batch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var patterns = new List<string>();
            if (_model.ConvertImageToPbp || _model.GenerateResourceFolders)
            {
                if (_model.Settings.IsM3uChecked)
                {
                    patterns.Add("*.m3u");
                }
                if (_model.Settings.IsBinChecked)
                {
                    patterns.Add("*.cue");
                    patterns.Add("*.bin");
                }
                if (_model.Settings.IsImgChecked)
                {
                    patterns.Add("*.img");
                }
                if (_model.Settings.IsIsoChecked)
                {
                    patterns.Add("*.iso");
                }
                if (_model.Settings.Is7zChecked)
                {
                    patterns.Add("*.7z");
                }
                if (_model.Settings.IsZipChecked)
                {
                    patterns.Add("*.zip");
                }
                if (_model.Settings.IsRarChecked)
                {
                    patterns.Add("*.rar");
                }
            }


            if (_model.ConvertPbpToImage || _model.ExtractResources || _model.GenerateResourceFolders)
            {
                patterns.Add("*.pbp");
            }




            Task.Run(() =>
            {
                _dispatcher.Invoke(() =>
                {
                    _model.IsScanning = true;
                    _model.BatchEntries.Clear();
                });

                var ignoreFileSet = new HashSet<string>();

                string GetAbsolutePath(string currentDirectory, string file)
                {
                    if (Path.IsPathFullyQualified(file)) return file;
                    return Path.Combine(currentDirectory, file);
                }


                string GetFullPath(string file)
                {
                    return Path.Combine(_model.Settings.InputPath, file);
                }

                var scanEntries = new List<ScanEntry>();

                foreach (var pattern in patterns)
                {
                    if (_token.IsCancellationRequested) break;

                    var searchOption = SearchOption.TopDirectoryOnly;

                    if (_model.Settings.RecurseFolders)
                    {
                        searchOption = SearchOption.AllDirectories;
                    }

                    var files = Directory.EnumerateFiles(_model.Settings.InputPath, pattern, searchOption);

                    foreach (var file in files)
                    {
                        if (_token.IsCancellationRequested) break;

                        if (ignoreFileSet.Contains(file))
                        {
                            continue;
                        }

                        try
                        {
                            var scanEntry = GetScanEntry(file);
                            var discPath = GetDiscPath(file);
                            var ignoreFiles = GetIgnoreFiles(scanEntry);

                            foreach (var ignoreFile in ignoreFiles)
                            {
                                ignoreFileSet.Add(ignoreFile);
                            }

                            var gameId = GameDB.FindGameId(discPath);

                            if (gameId != null)
                            {
                                var gameEntry = _gameDb.GetEntryByGameID(gameId);

                                scanEntry.GameEntry = gameEntry;
                            }

                            scanEntries.Add(scanEntry);

                        }
                        catch (InvalidFileSystemException)
                        {

                        }
                        catch (Exception e)
                        {
                            scanEntries.Add(new ScanEntry()
                            {
                                Path = file,
                                HasError = true,
                                ErrorMesage = e.Message
                            });
                        }



                        //if (pattern != "*.m3u")
                        //{

                        //    if (gameEntry != null)
                        //    {
                        //        if (_model.Settings.MergeMultiDiscs)
                        //        {
                        //            // gamesByMainGameId
                        //        }
                        //    }
                        //}





                    }
                }

                //scanEntries = scanEntries.Where(d => ignoreFileSet.Contains(d.Path)).ToList();

                //var gameGroups = scanEntries.Where(d => Path.GetExtension(d.Path).ToLower() != ".m3u").Where(d => d.GameEntry != null).GroupBy(d => d.GameEntry.MainGameID);

                //var multiDiscGames = gameGroups.Where(d => d.Count() > 1).ToList();


                //if (multiDiscGames.Any())
                //{
                //    foreach (var multiDiscGame in multiDiscGames)
                //    {
                //        var discs = multiDiscGame.Select(d => d).OrderBy(d => d.GameEntry.DiscIndex).ToList();

                //        var lowestCommonPath = GetLowestCommonFolder(discs.Select(d => d.Path).ToArray());

                //        var insertIndex = scanEntries.IndexOf(discs[0]);


                //        var playlist = new List<string>();


                //        foreach (var scanEntry in discs)
                //        {
                //            playlist.Add(Path.GetRelativePath(lowestCommonPath, scanEntry.Path));
                //            ignoreFileSet.Add(scanEntry.Path);
                //        }

                //        var m3uPath = Path.Combine(lowestCommonPath, $"{discs[0].GameEntry.MainGameTitle}.m3u");

                //        File.WriteAllText(m3uPath, string.Join("\n", playlist));

                //        var newEntry = new ScanEntry() { GameEntry = discs[0].GameEntry, Path = m3uPath };

                //        scanEntries.Insert(insertIndex, newEntry);
                //    }
                //}


                foreach (var batchEntry in scanEntries.OrderBy(d => d.GameEntry?.MainGameTitle))
                {
                    if (!ignoreFileSet.Contains(batchEntry.Path))
                    {
                        var relativePath = Path.GetRelativePath(_model.Settings.InputPath, batchEntry.Path);
                        _dispatcher.Invoke(() =>
                        {
                            _model.BatchEntries.Add(new BatchEntryModel()
                            {
                                IsSelected = true,
                                RelativePath = relativePath,
                                MainGameId = batchEntry.GameEntry?.MainGameID,
                                GameId = batchEntry.GameEntry?.GameID,
                                MaxProgress = 100,
                                Progress = 0,
                                Status = "Ready",
                                Type = batchEntry.Type,
                                SubEntries = batchEntry.SubEntries
                            });
                        });
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

                _dispatcher.Invoke(() =>
                {
                    MessageBox.Show(Window, $"Scan found {_model.BatchEntries.Count} entries", "Batch", MessageBoxButton.OK, MessageBoxImage.Information);
                });

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

            if (string.IsNullOrEmpty(_model.Settings.OutputPath))
            {
                MessageBox.Show(Window, "No output path specified", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Directory.Exists(_model.Settings.OutputPath))
            {
                MessageBox.Show(Window, "Invalid directory or directory not found", "Batch", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_model.BatchEntries.Count == 0)
            {
                MessageBox.Show(Window, "Nothing to process. Please Scan a directory first.", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var jobs = _model.BatchEntries.Where(e => e.Status != "Complete" && e.IsSelected).ToList();

            if (jobs.Count == 0)
            {
                MessageBox.Show(Window, "Nothing to process. Please select one or more valid items to process.", "Batch", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


            var processor = new Processor(_dispatcher, _gameDb, _settings, new ProcessEventHandler());

            foreach (var entry in jobs)
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
                else
                {
                    _dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Window, "One or more errors occured during conversion", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
                    });
                }

            });
        }

        private void BrowseInput(object obj)
        {
            var folderBrowserDialog = new Microsoft.Win32.OpenFolderDialog();
            var result = folderBrowserDialog.ShowDialog(Window);
            if (result is true)
            {
                _model.Settings.InputPath = folderBrowserDialog.FolderName;
            }
        }

        private void BrowseOutput(object obj)
        {
            var folderBrowserDialog = new Microsoft.Win32.OpenFolderDialog();
            var result = folderBrowserDialog.ShowDialog(Window);
            if (result is true)
            {
                _model.Settings.OutputPath = folderBrowserDialog.FolderName;
            }
        }

        public void UpdateToken(CancellationToken token)
        {
            _token = token;
        }
    }
}

