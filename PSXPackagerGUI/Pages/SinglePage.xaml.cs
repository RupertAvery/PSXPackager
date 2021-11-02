using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Popstation;
using Popstation.Database;
using Popstation.Pbp;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Controls;
using PSXPackagerGUI.Models;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Single.xaml
    /// </summary>
    public partial class SinglePage : Page
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Model _viewModel;
        private readonly SettingsModel _settings;
        private readonly GameDB _gameDb;

        private IEnumerable<Disc> DummyDisc(int start, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return Disc.EmptyDisc(start + i);
            }
        }

        public void OnClosing(CancelEventArgs e)
        {
            if (IsBusy)
            {
                var result = MessageBox.Show("An operation is in progress. Are you sure you want to cancel?", "PSXPackager",
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

        public SinglePage(SettingsModel settings, GameDB gameDb)
        {
            _settings = settings;
            _gameDb = gameDb;
            _cancellationTokenSource = new CancellationTokenSource();

            InitializeComponent();

            _viewModel = new Model
            {
                Icon0 = new ResourceModel() { Type = ResourceType.ICON0 },
                Icon1 = new ResourceModel() { Type = ResourceType.ICON1 },
                Pic0 = new ResourceModel() { Type = ResourceType.PIC0 },
                Pic1 = new ResourceModel() { Type = ResourceType.PIC1 },
                Snd0 = new ResourceModel() { Type = ResourceType.SND0 },
                IsDirty = false,
                MaxProgress = 100,
                Progress = 0,
                Discs = DummyDisc(0, 5).ToList()
            };

            DataContext = _viewModel;

            //Closing += OnClosing;
        }



        public void LoadPbp(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var pbpReader = new PbpReader(stream);

                    var discs = pbpReader.Discs.Select((d, i) =>
                    {
                        var game = _gameDb.GetEntryByScannerID(d.DiscID);

                        var disc = new Disc()
                        {
                            Index = i,
                            Title = game.GameName,
                            Size = d.IsoSize,
                            GameID = d.DiscID,
                            IsRemoveEnabled = true,
                            IsLoadEnabled = true,
                            IsSaveAsEnabled = true,
                            IsEmpty = false,
                            SourceUrl = $"//pbp/disc{i}/{path}"
                        };

                        disc.RemoveCommand = new RelayCommand((o) => Remove(disc));

                        return disc;
                    }).ToList();


                    var dummyDiscs = DummyDisc(discs.Count, 5 - discs.Count);

                    _viewModel.Discs = discs.Concat(dummyDiscs).ToList();


                    void LoadResource(ResourceType type, ResourceModel model)
                    {
                        if (pbpReader.TryGetResourceStream(type, stream, out var resourceStream))
                        {
                            model.Icon = GetBitmapImage(resourceStream);
                            model.IsLoadEnabled = true;
                            model.IsSaveAsEnabled = true;
                            model.IsRemoveEnabled = true;
                            model.SourceUrl = $"//pbp/{type.ToString().ToLower()}/{path}";
                        }
                    }


                    LoadResource(ResourceType.ICON0, _viewModel.Icon0);
                    LoadResource(ResourceType.ICON1, _viewModel.Icon1);
                    LoadResource(ResourceType.PIC0, _viewModel.Pic0);
                    LoadResource(ResourceType.PIC1, _viewModel.Pic1);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(Window, e.Message, "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }


        private void Remove(Disc disc)
        {
            _viewModel.Discs = _viewModel.Discs.Select(d => d == disc ? Disc.EmptyDisc(d.Index) : d).ToList();
            _viewModel.IsDirty = true;
        }

        public static BitmapImage GetBitmapImage(Stream stream)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private DiscInfo GetDiscInfo(Disc disc)
        {
            var game = _gameDb.GetEntryByScannerID(disc.GameID);

            var cueFilename = Path.GetFileNameWithoutExtension(disc.SourceUrl) + ".cue";
            var dirPath = Path.GetDirectoryName(disc.SourceUrl);
            var cuePath = Path.Combine(dirPath, cueFilename);

            return new DiscInfo()
            {
                GameID = game.ScannerID,
                GameTitle = game.SaveDescription,
                GameName = game.GameName,
                Region = game.Format,
                MainGameID = game.SaveFolderName,
                SourceIso = disc.SourceUrl,
                SourceToc = cuePath,
            };
        }

        private Resource GetResource(ResourceModel resource)
        {
            // If resource is from PBP
            return resource.SourceUrl == null ? Resource.Empty(resource.Type) : new Resource(resource.Type, resource.SourceUrl);
        }


        private Window Window => Window.GetWindow(this);

        public bool IsBusy => _viewModel.IsBusy;

        public void Save()
        {
            var discs = _viewModel.Discs.Where(d => d.SourceUrl != null).OrderBy(d => d.Index).ToList();

            if (discs.Count == 0)
            {
                MessageBox.Show(Window, "No discs have been added!", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var discIndexes = discs.Select(d => d.Index);

            var expectedIndex = 0;

            foreach (var discIndex in discIndexes)
            {
                if (discIndex != expectedIndex)
                {
                    if (expectedIndex == 0)
                    {
                        MessageBox.Show(Window, "First disc should not be empty!", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    MessageBox.Show(Window, "Should not have empty disc between discs!", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                expectedIndex++;
            }


            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            saveFileDialog.AddExtension = true;

            saveFileDialog.Filter = "EBOOT files|*.pbp|All files|*.*";
            saveFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                string format = "";

                var game = _gameDb.GetEntryByScannerID(_viewModel.Discs.First().GameID);
                var appPath = ApplicationInfo.AppPath;

                var options = new ConvertOptions()
                {
                    OutputPath = Path.GetDirectoryName(saveFileDialog.FileName),
                    OriginalFilename = Path.GetFileName(saveFileDialog.FileName),
                    DiscInfos = discs.Select(GetDiscInfo).ToList(),
                    Icon0 = GetResource(_viewModel.Icon0),
                    Icon1 = GetResource(_viewModel.Icon1),
                    Pic0 = GetResource(_viewModel.Pic0),
                    Pic1 = GetResource(_viewModel.Pic1),
                    Snd0 = GetResource(_viewModel.Snd0),
                    MainGameTitle = game.SaveDescription,
                    MainGameID = game.SaveFolderName,
                    MainGameRegion = game.Format,
                    SaveTitle = game.SaveDescription,
                    SaveID = game.SaveFolderName,
                    BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                    CompressionLevel = _settings.CompressionLevel,
                    //CheckIfFileExists = processOptions.CheckIfFileExists,
                    //SkipIfFileExists = processOptions.SkipIfFileExists,
                    FileNameFormat = _settings.FileNameFormat,
                };

                PbpWriter writer;
                writer = discs.Count > 1
                    ? (PbpWriter)new MultiDiscPbpWriter(options)
                    : new SingleDiscPbpWriter(options);

                writer.Notify += Notify;

                Task.Run(() =>
                {
                    using (var stream =
                        new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        writer.Write(stream, _cancellationTokenSource.Token);
                    }
                });
            }
        }

        private long _total;
        private string _action;
        private double _lastvalue;

        private void Notify(PopstationEventEnum @event, object value)
        {
            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                    break;

                case PopstationEventEnum.ProcessingComplete:
                    _viewModel.MaxProgress = 100;
                    _viewModel.Progress = 0;
                    _viewModel.IsBusy = false;
                    break;

                case PopstationEventEnum.Error:
                    break;

                case PopstationEventEnum.FileName:
                case PopstationEventEnum.Info:
                    break;

                case PopstationEventEnum.Warning:
                    break;

                case PopstationEventEnum.GetIsoSize:
                    _lastvalue = 0;
                    _viewModel.MaxProgress = (uint)value;
                    _viewModel.Progress = 0;
                    break;

                case PopstationEventEnum.ConvertSize:
                case PopstationEventEnum.ExtractSize:
                case PopstationEventEnum.WriteSize:
                    _lastvalue = 0;
                    _viewModel.MaxProgress = (uint)value;
                    _viewModel.Progress = 0;
                    break;

                case PopstationEventEnum.ConvertStart:
                    _action = "Converting";
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.WriteStart:
                    _action = "Writing";
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.ExtractStart:
                    _action = "Extracting";
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.DecompressStart:
                    _action = "Decompressing";
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.ExtractComplete:
                case PopstationEventEnum.WriteComplete:
                case PopstationEventEnum.DecompressComplete:
                    _viewModel.MaxProgress = 100;
                    _viewModel.Progress = 0;
                    _viewModel.IsBusy = false;
                    break;

                case PopstationEventEnum.ConvertComplete:
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(Window, "Conversion complete!", "PSXPackager", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                    break;


                case PopstationEventEnum.ConvertProgress:
                case PopstationEventEnum.ExtractProgress:
                case PopstationEventEnum.WriteProgress:
                    Dispatcher.Invoke(() =>
                    {
                        var percent = (uint)value / (float)_viewModel.MaxProgress * 100f;
                        if (percent - _lastvalue >= 0.25)
                        {
                            _viewModel.Status = $"{_action} ({percent:F0}%)";
                            _viewModel.Progress = (uint)value;
                            _viewModel.IsBusy = false;
                            _lastvalue = percent;
                        }
                    });

                    break;

                case PopstationEventEnum.DecompressProgress:
                    break;
            }
        }

        private static string[] imageExtensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp" };

        private void Icon0_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                _viewModel.Icon0.Icon = GetBitmapImage(GetImageStream(filename));
                _viewModel.Icon0.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Icon1_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                _viewModel.Icon1.Icon = GetBitmapImage(GetImageStream(filename));
                _viewModel.Icon1.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Pic0_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                _viewModel.Pic0.Icon = GetBitmapImage(GetImageStream(filename));
                _viewModel.Pic0.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Pic1_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                _viewModel.Pic1.Icon = GetBitmapImage(GetImageStream(filename));
                _viewModel.Pic1.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Snd0_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, new[] { ".at3" }, out var filename))
            {
                //_viewModel.Snd0.Icon = GetBitmapImage(GetImageStream(filename));
                _viewModel.Snd0.SourceUrl = filename;

            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private bool TryGetFilename(IDataObject data, string[] allowedExtensions, out string filename)
        {
            filename = ((string[])data.GetData("FileName"))[0];
            if (allowedExtensions.Contains(Path.GetExtension(filename).ToLower()))
            {
                return true;
            }
            return false;
        }

        private Stream GetImageStream(string filename)
        {
            return new FileStream(filename, FileMode.Open, FileAccess.Read);
        }

        private Stream GetDataStream(IDataObject data)
        {
            var filename = ((string[])data.GetData("FileName"))[0];
            return new FileStream(filename, FileMode.Open, FileAccess.Read);
        }

        private void DiscButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var context = button.DataContext as Disc;

            var cm = this.FindResource("DiscButtonContextMenu") as ContextMenu;
            var menuItems = cm.Items.OfType<MenuItem>();
            foreach (var menuItem in menuItems)
            {
                menuItem.DataContext = button.DataContext;
                switch (menuItem.Name)
                {
                    case "LoadISO":
                        menuItem.IsEnabled = context.IsLoadEnabled;
                        break;
                    case "SaveImage":
                        menuItem.IsEnabled = context.IsSaveAsEnabled;
                        break;
                }
            }

            cm.PlacementTarget = button;
            cm.IsOpen = true;
        }

        private void LoadISO_OnClick(object sender, RoutedEventArgs e)
        {
            var disc = ((MenuItem)sender).DataContext as Disc;
            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            saveFileDialog.Filter = "Supported files|*.bin;*.iso;*.img|All files|*.*";
            saveFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                var gameId = GameDB.FindGameId(saveFileDialog.FileName);
                var game = _gameDb.GetEntryByScannerID(gameId);
                var fileInfo = new FileInfo(saveFileDialog.FileName);

                disc.SourceUrl = saveFileDialog.FileName;
                disc.Size = (uint)fileInfo.Length;
                disc.GameID = gameId;
                disc.Title = game.GameName;
                disc.IsEmpty = false;
                disc.IsRemoveEnabled = true;
                disc.IsLoadEnabled = true;
                disc.IsSaveAsEnabled = false;
                disc.RemoveCommand = new RelayCommand((o) => Remove(disc));

                _viewModel.IsDirty = true;
            }

        }

        private void SaveImage_OnClick(object sender, RoutedEventArgs e)
        {
            var context = ((MenuItem)sender).DataContext as Disc;
            var pbpRegex = new Regex("//pbp/disc(\\d)/(.*\\.pbp)", RegexOptions.IgnoreCase);

            var game = _gameDb.GetEntryByScannerID(context.GameID);


            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.FileName = $"{game.GameName}.bin";
            saveFileDialog.Filter = "BIN files|*.bin|All files|*.*";
            saveFileDialog.DefaultExt = ".bin";
            saveFileDialog.AddExtension = true;
            var result = saveFileDialog.ShowDialog(Window);

            if (result.GetValueOrDefault(false))
            {
                var sourceUrl = _viewModel.Discs.Single(d => d.Index == context.Index).SourceUrl;

                var match = pbpRegex.Match(sourceUrl);
                if (match.Success)
                {
                    Task.Run(() =>
                    {
                        using (var stream = new FileStream(match.Groups[2].Value, FileMode.Open, FileAccess.Read))
                        {
                            var pbpReader = new PbpReader(stream);
                            using (var output = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate,
                                FileAccess.Write))
                            {
                                var disc = pbpReader.Discs[int.Parse(match.Groups[1].Value)];
                                _viewModel.Status = "Extracting disc image...";
                                _viewModel.MaxProgress = disc.IsoSize;
                                _viewModel.IsBusy = true;
                                _lastvalue = 0;
                                disc.ProgressEvent = ProgressEvent;
                                disc.CopyTo(output, _cancellationTokenSource.Token);

                                var cueFile = TOCHelper.TOCtoCUE(disc.TOC, Path.GetFileName(saveFileDialog.FileName));


                                var cueFilename = Path.GetFileNameWithoutExtension(saveFileDialog.FileName) + ".cue";
                                var dirPath = Path.GetDirectoryName(saveFileDialog.FileName);
                                var cuePath = Path.Combine(dirPath, cueFilename);

                                CueFileWriter.Write(cueFile, cuePath);

                                _viewModel.Status = "";
                                _viewModel.MaxProgress = 100;
                                _viewModel.Progress = 0;
                                _viewModel.IsBusy = false;
                                Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show(Window, $"Disc image has been extracted to \"{saveFileDialog.FileName}\"",
                                        "PSXPackager",
                                        MessageBoxButton.OK, MessageBoxImage.Information);
                                });
                            }
                        }
                    });
                }
            }


        }

        private void ProgressEvent(uint progress)
        {
            var percent = progress / (float)_viewModel.MaxProgress * 100f;
            if (percent - _lastvalue > 0.25)
            {
                _viewModel.Progress = progress;
                _viewModel.Status = $"Extracting disc image... ({percent:F0}%)";
                _lastvalue = percent;
            }
        }

        private void Resource_OnMore(object sender, RoutedEventArgs e)
        {
            var control = sender as ResourceControl;
            var context = control.DataContext as ResourceModel;

            var cm = this.FindResource("ResourceButtonContextMenu") as ContextMenu;
            var menuItems = cm.Items.OfType<MenuItem>();
            foreach (var menuItem in menuItems)
            {
                menuItem.DataContext = control.DataContext;
                switch (menuItem.Name)
                {
                    case "LoadResource":
                        menuItem.IsEnabled = context.IsLoadEnabled;
                        break;
                    case "SaveResource":
                        menuItem.IsEnabled = context.IsSaveAsEnabled;
                        break;
                }
            }

            cm.PlacementTarget = control;
            cm.IsOpen = true;
        }

        private void LoadResource_OnClick(object sender, RoutedEventArgs e)
        {
            var context = ((MenuItem)sender).DataContext as ResourceModel;
            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();

            switch (context.Type)
            {
                case ResourceType.ICON0:
                case ResourceType.PIC0:
                case ResourceType.PIC1:
                    openFileDialog.Filter = "Supported files|*.png|All files|*.*";
                    break;
                case ResourceType.ICON1:
                    openFileDialog.Filter = "Supported files|*.png;*.pmf|All files|*.*";
                    break;
                case ResourceType.SND0:
                    openFileDialog.Filter = "Supported files|*.at3|All files|*.*";
                    break;
            }

            openFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(openFileDialog.FileName))
            {
                var fileStream = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);
                context.Icon = GetBitmapImage(fileStream);
                context.IsLoadEnabled = true;
                context.IsSaveAsEnabled = false;
                context.IsRemoveEnabled = true;
                context.SourceUrl = openFileDialog.FileName;
                _viewModel.IsDirty = true;
            }

        }


        private void SaveResource_OnClick(object sender, RoutedEventArgs e)
        {
            var context = ((MenuItem)sender).DataContext as ResourceModel;
            var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
            saveFileDialog.AddExtension = true;

            switch (context.Type)
            {
                case ResourceType.ICON0:
                case ResourceType.PIC0:
                case ResourceType.PIC1:
                    saveFileDialog.Filter = "Supported files|*.png|All files|*.*";
                    break;
                case ResourceType.ICON1:
                    saveFileDialog.Filter = "Supported files|*.png;*.pmf|All files|*.*";
                    break;
                case ResourceType.SND0:
                    saveFileDialog.Filter = "Supported files|*.at3|All files|*.*";
                    break;
            }
            saveFileDialog.ShowDialog();

            if (!string.IsNullOrEmpty(saveFileDialog.FileName))
            {
                var pbpRegex = new Regex("//pbp/(.*?)/(.*\\.pbp)", RegexOptions.IgnoreCase);

                var match = pbpRegex.Match(context.SourceUrl);

                if (match.Success)
                {
                    Task.Run(() =>
                    {
                        using (var stream = new FileStream(match.Groups[2].Value, FileMode.Open, FileAccess.Read))
                        {
                            var pbpReader = new PbpReader(stream);
                            using (var output = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate,
                                FileAccess.Write))
                            {
                                var type = match.Groups[1].Value switch
                                {
                                    "icon0" => ResourceType.ICON0,
                                    "icon1" => ResourceType.ICON1,
                                    "pic0" => ResourceType.PIC0,
                                    "pic1" => ResourceType.PIC1,
                                    "snd0" => ResourceType.SND0,
                                };

                                if (pbpReader.TryGetResourceStream(type, stream, out var resourceStream))
                                {
                                    resourceStream.CopyTo(output);
                                    resourceStream.Dispose();
                                    Dispatcher.Invoke(() =>
                                    {
                                        MessageBox.Show(Window, $"Resource has been extracted to \"{saveFileDialog.FileName}\"", "PSXPackager",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                    });
                                }
                                //_viewModel.MaxProgress = disc.IsoSize;
                                //_viewModel.IsBusy = true;
                                //disc.ProgressEvent = ProgressEvent;
                                //disc.CopyTo(output, cancellationTokenSource.Token);
                                //_viewModel.MaxProgress = 100;
                                //_viewModel.Progress = 0;
                                //_viewModel.IsBusy = false;
                            }
                        }
                    });
                }
            }
        }

        private void Resource_OnRemove(object sender, RoutedEventArgs e)
        {
            var context = ((ResourceControl)sender).DataContext as ResourceModel;
            context.Icon = null;
            context.IsSaveAsEnabled = false;
            context.SourceUrl = null;
            _viewModel.IsDirty = true;
        }

    }
}
