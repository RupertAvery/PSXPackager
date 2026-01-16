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
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Popstation;
using Popstation.Database;
using Popstation.Pbp;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Controls;
using PSXPackagerGUI.Models;
using SFOEntry = PSXPackagerGUI.Models.SFOEntry;

namespace PSXPackagerGUI.Pages
{
    /// <summary>
    /// Interaction logic for Single.xaml
    /// </summary>
    public partial class SinglePage : Page, INotifier
    {
        private CancellationTokenSource _cancellationTokenSource;
        private SingleModel _model;
        private readonly Window _window;
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

        public SinglePage(Window window, SettingsModel settings, GameDB gameDb)
        {
            _window = window;
            _settings = settings;
            _gameDb = gameDb;
            _cancellationTokenSource = new CancellationTokenSource();

            InitializeComponent();

            Model = new SingleModel
            {
                Icon0 = new ResourceModel() { Type = ResourceType.ICON0 },
                Icon1 = new ResourceModel() { Type = ResourceType.ICON1 },
                Pic0 = new ResourceModel() { Type = ResourceType.PIC0 },
                Pic1 = new ResourceModel() { Type = ResourceType.PIC1 },
                Snd0 = new ResourceModel() { Type = ResourceType.SND0 },
                IsDirty = false,
                MaxProgress = 100,
                Progress = 0,
            };


            DataContext = Model;

            ResetModel();

            //Closing += OnClosing;
        }


        private void ResetModel()
        {
            Model.Icon0.Reset();
            Model.Icon1.Reset();
            Model.Pic0.Reset();
            Model.Pic1.Reset();
            Model.Snd0.Reset();
            Model.IsDirty = false;
            Model.MaxProgress = 100;
            Model.Progress = 0;
            Model.Discs = new ObservableCollection<Disc>(DummyDisc(0, 5));
            Model.IsNew = true;
            Model.SFOEntries = new ObservableCollection<SFOEntry>()
            {
                new() { Key = SFOKeys.BOOTABLE, Value = 0x01, EntryType = SFOEntryType.NUM, IsEditable = false },
                new() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category, EntryType = SFOEntryType.STR, IsEditable = false  },
                new() { Key = SFOKeys.DISC_ID, Value = "", EntryType = SFOEntryType.STR,IsEditable = true  },
                new() { Key = SFOKeys.DISC_VERSION, Value =  "1.00", EntryType = SFOEntryType.STR, IsEditable = true  },
                new() { Key = SFOKeys.LICENSE, Value =  SFOValues.License, EntryType = SFOEntryType.STR,IsEditable = true  },
                new() { Key = SFOKeys.PARENTAL_LEVEL, Value =  0x01 , EntryType = SFOEntryType.NUM, IsEditable = false },
                new() { Key = SFOKeys.PSP_SYSTEM_VER, Value =  "3.01", EntryType = SFOEntryType.STR,IsEditable = true  },
                new() { Key = SFOKeys.REGION, Value =  0x8000, EntryType = SFOEntryType.NUM, IsEditable = true },
                new() { Key = SFOKeys.TITLE, Value = "", EntryType = SFOEntryType.STR, IsEditable = true  },
            };
        }

        public void LoadPbp()
        {
            if (IsBusy)
            {
                MessageBox.Show(Window, "An operation is in progress. Please wait for the current operation to complete.", "PSXPackager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var openFileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            openFileDialog.Filter = "PBP Files|*.pbp|All files|*.*";
            var result = openFileDialog.ShowDialog();

            if (!result.GetValueOrDefault(false))
            {
                return;
            }

            var path = openFileDialog.FileName;

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

                    Model.Discs = new ObservableCollection<Disc>(discs.Concat(dummyDiscs));


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


                    LoadResource(ResourceType.ICON0, Model.Icon0);
                    LoadResource(ResourceType.ICON1, Model.Icon1);
                    LoadResource(ResourceType.PIC0, Model.Pic0);
                    LoadResource(ResourceType.PIC1, Model.Pic1);

                    Model.SFOEntries = new ObservableCollection<SFOEntry>();

                    foreach (var sfoDataEntry in pbpReader.SFOData.Entries)
                    {
                        Model.SFOEntries.Add(new SFOEntry()
                        {
                            Key = sfoDataEntry.Key,
                            Value = sfoDataEntry.Value,
                            IsEditable = GetIsEditable(sfoDataEntry.Key),
                            EntryType = GetEntryType(sfoDataEntry.Key),
                        });
                    }


                    Model.IsNew = false;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(Window, e.Message, "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }

        bool GetIsEditable(string key)
        {
            return key switch
            {
                SFOKeys.BOOTABLE => false,
                SFOKeys.CATEGORY => false,
                SFOKeys.DISC_ID => true,
                SFOKeys.DISC_VERSION => true,
                SFOKeys.LICENSE => true,
                SFOKeys.PARENTAL_LEVEL => false,
                SFOKeys.PSP_SYSTEM_VER => true,
                SFOKeys.REGION => true,
                SFOKeys.TITLE => true,
                _ => false
            };
        }


        SFOEntryType GetEntryType(string key)
        {
            return key switch
            {
                SFOKeys.BOOTABLE => SFOEntryType.NUM,
                SFOKeys.CATEGORY => SFOEntryType.STR,
                SFOKeys.DISC_ID => SFOEntryType.STR,
                SFOKeys.DISC_VERSION => SFOEntryType.STR,
                SFOKeys.LICENSE => SFOEntryType.STR,
                SFOKeys.PARENTAL_LEVEL => SFOEntryType.NUM,
                SFOKeys.PSP_SYSTEM_VER => SFOEntryType.STR,
                SFOKeys.REGION => SFOEntryType.NUM,
                SFOKeys.TITLE => SFOEntryType.STR,
                _ => SFOEntryType.STR
            };
        }


        bool ValidateSFOEntry(SFOEntry entry)
        {
            bool isValid = true;

            var format = GetFormat(entry.Key);

            if (format == "string")
            {
                if (((string)entry.Value).Length > GetMaxLength(entry.Key))
                {
                    isValid = false;
                }

                //if (!ValidateFormat(entry.Key, (string)entry.Value))
                //{
                //    isValid = false;
                //}
            }
            if (format == "uint")
            {
            }

            return isValid;
        }

        //private bool ValidateFormat(string key, string value)
        //{
        //    if (key == SFOKeys.DISC_VERSION)
        //    {
        //        return true;
        //    }

        //    if (key == SFOKeys.PSP_SYSTEM_VER)
        //    {
        //        return true;
        //    }

        //    return true;
        //}

        private string GetFormat(string key)
        {
            return key switch
            {
                SFOKeys.BOOTABLE => "uint",
                SFOKeys.CATEGORY => "string",
                SFOKeys.DISC_ID => "string",
                SFOKeys.DISC_VERSION => "string",
                SFOKeys.LICENSE => "string",
                SFOKeys.PARENTAL_LEVEL => "uint",
                SFOKeys.PSP_SYSTEM_VER => "string",
                SFOKeys.REGION => "uint",
                SFOKeys.TITLE => "string",
                _ => throw new ArgumentOutOfRangeException()
            };
        }


        private uint GetMaxLength(string key)
        {
            return key switch
            {
                SFOKeys.BOOTABLE => 4,
                SFOKeys.CATEGORY => 4,
                SFOKeys.DISC_ID => 16,
                SFOKeys.DISC_VERSION => 8,
                SFOKeys.LICENSE => 512,
                SFOKeys.PARENTAL_LEVEL => 4,
                SFOKeys.PSP_SYSTEM_VER => 8,
                SFOKeys.REGION => 4,
                SFOKeys.TITLE => 128,
                _ => throw new ArgumentOutOfRangeException()
            };
        }



        private void Remove(Disc disc)
        {
            Model.Discs[disc.Index] = Disc.EmptyDisc(disc.Index);
            Model.IsDirty = true;
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


            return new DiscInfo()
            {
                GameID = game.ScannerID,
                GameTitle = game.SaveDescription,
                GameName = game.GameName,
                Region = game.Format,
                MainGameID = game.SaveFolderName,
                SourceIso = disc.SourceUrl,
                SourceToc = disc.SourceTOC,
            };
        }

        private Resource GetResource(ResourceModel resource)
        {
            // If resource is from PBP
            return resource.SourceUrl == null ? Resource.Empty(resource.Type) : new Resource(resource.Type, resource.SourceUrl);
        }


        private Resource GetResourceOrDefault(ResourceType type, string ext, ResourceModel resource)
        {
            // If resource is from PBP
            var defaultUrl = Path.Combine(PSXPackager.Common.ApplicationInfo.AppPath, "Resources", $"{type}.{ext}");

            return resource.SourceUrl == null ? new Resource(resource.Type, defaultUrl) : new Resource(resource.Type, resource.SourceUrl);
        }

        private Window Window => _window;

        public bool IsBusy => Model.IsBusy;

        public SingleModel Model
        {
            get => _model;
            set => _model = value;
        }

        public void Save(bool pspMode = false)
        {
            if (!Model.IsNew)
            {
                MessageBox.Show(Window, "Modifying and saving existing PBPs is not supported yet.", "PSXPackager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (IsBusy)
            {
                MessageBox.Show(Window, "An operation is in progress. Please wait for the current operation to complete.", "PSXPackager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var discs = Model.Discs.Where(d => d.SourceUrl != null).OrderBy(d => d.Index).ToList();

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

            var filename = "";

            var gameId = Model.Discs.First().GameID;

            if (pspMode)
            {
                var ebootPath = Path.Combine(gameId, "EBOOT.PBP");

                MessageBox.Show(Window, $"Select the folder to save {ebootPath}", "Save for PSP",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                var selectFolderDialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();

                selectFolderDialog.ShowDialog();

                filename = Path.Combine(selectFolderDialog.SelectedPath, ebootPath);

                if (File.Exists(filename))
                {
                    var result = MessageBox.Show(Window, $"The file {filename} exists! Overwrite?", "Save for PSP", MessageBoxButton.YesNo, MessageBoxImage.Warning,
                        MessageBoxResult.No);

                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }
            else
            {
                var saveFileDialog = new Ookii.Dialogs.Wpf.VistaSaveFileDialog();
                saveFileDialog.AddExtension = true;
                saveFileDialog.DefaultExt = ".pbp";
                saveFileDialog.Filter = "EBOOT files|*.pbp|All files|*.*";
                saveFileDialog.ShowDialog();
                filename = saveFileDialog.FileName;
            }



            if (!string.IsNullOrEmpty(filename))
            {
                var game = _gameDb.GetEntryByScannerID(Model.Discs.First().GameID);
                var appPath = ApplicationInfo.AppPath;

                var options = new ConvertOptions()
                {
                    OutputPath = Path.GetDirectoryName(filename),
                    OriginalFilename = Path.GetFileName(filename),
                    DiscInfos = discs.Select(GetDiscInfo).ToList(),
                    Icon0 = GetResourceOrDefault(ResourceType.ICON0, "png", Model.Icon0),
                    Icon1 = GetResource(Model.Icon1),
                    Pic0 = GetResourceOrDefault(ResourceType.PIC0, "png", Model.Pic0),
                    Pic1 = GetResourceOrDefault(ResourceType.PIC1, "png", Model.Pic1),
                    Snd0 = GetResource(Model.Snd0),
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
                    SFOEntries = Model.SFOEntries.Select(ToSFOEntry).ToList()
                };

                PbpWriter writer;
                writer = discs.Count > 1
                    ? new MultiDiscPbpWriter(options)
                    : new SingleDiscPbpWriter(options);

                writer.Notify += Notify;

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                Task.Run(() =>
                {
                    var processing = new Popstation.Processing(this, null, null);

                    var cueFiles = options.DiscInfos.Where(d => Path.GetExtension(d.SourceIso).ToLower() == ".cue");

                    foreach (var discInfo in cueFiles)
                    {
                        var (binfile, cuefile) = processing.ProcessCue(discInfo.SourceIso, Path.GetTempPath());
                        discInfo.SourceIso = binfile;
                        discInfo.SourceToc = cuefile;
                    }

                    try
                    {
                        Dispatcher.Invoke(() => { Model.IsBusy = true; });

                        var parentPath = Path.GetDirectoryName(filename);

                        if (!Directory.Exists(filename))
                        {
                            Directory.CreateDirectory(parentPath);
                        }

                        using (var stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            writer.Write(stream, _cancellationTokenSource.Token);
                        }

                        if (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(Window, $"EBOOT has been saved to \"{filename}\"",
                                    "PSXPackager",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show(Window, $"The operation was cancelled",
                                    "PSXPackager",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            });
                        }


                    }
                    catch (Exception e)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(Window, e.Message,
                                "PSXPackager",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    finally
                    {
                        processing.Cleanup();

                        Dispatcher.Invoke(() => { Model.IsBusy = false; });
                    }
                });
            }
        }

        private string _action;
        private double _lastvalue;

        private Popstation.Pbp.SFOEntry ToSFOEntry(SFOEntry entry)
        {
            return new Popstation.Pbp.SFOEntry()
            {
                Key = entry.Key,
                Value = entry.Value
            };
        }

        public void Notify(PopstationEventEnum @event, object value)
        {
            switch (@event)
            {
                case PopstationEventEnum.ProcessingStart:
                    break;

                case PopstationEventEnum.ProcessingComplete:
                    Model.MaxProgress = 100;
                    Model.Progress = 0;
                    Model.IsBusy = false;
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
                    Model.MaxProgress = (uint)value;
                    Model.Progress = 0;
                    break;

                case PopstationEventEnum.ConvertSize:
                case PopstationEventEnum.ExtractSize:
                case PopstationEventEnum.WriteSize:
                    _lastvalue = 0;
                    Model.MaxProgress = (uint)value;
                    Model.Progress = 0;
                    break;

                case PopstationEventEnum.ConvertStart:
                    _action = "Converting";
                    Dispatcher.Invoke(() =>
                    {
                        Model.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.DiscStart:
                    _action = $"Writing Disc {value}";
                    Dispatcher.Invoke(() =>
                    {
                        Model.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.ExtractStart:
                    _action = "Extracting";
                    Dispatcher.Invoke(() =>
                    {
                        Model.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.DecompressStart:
                    _action = "Decompressing";
                    Dispatcher.Invoke(() =>
                    {
                        Model.IsBusy = true;
                    });
                    break;

                case PopstationEventEnum.ExtractComplete:
                case PopstationEventEnum.DiscComplete:
                case PopstationEventEnum.DecompressComplete:
                    Model.MaxProgress = 100;
                    Model.Progress = 0;
                    Model.IsBusy = false;
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
                        var percent = (uint)value / (float)Model.MaxProgress * 100f;
                        if (percent - _lastvalue >= 0.25)
                        {
                            Model.Status = $"{_action} ({percent:F0}%)";
                            Model.Progress = (uint)value;
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
                Model.Icon0.Icon = GetBitmapImage(GetImageStream(filename));
                Model.Icon0.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Icon1_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                Model.Icon1.Icon = GetBitmapImage(GetImageStream(filename));
                Model.Icon1.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Pic0_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                Model.Pic0.Icon = GetBitmapImage(GetImageStream(filename));
                Model.Pic0.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Pic1_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, imageExtensions, out var filename))
            {
                Model.Pic1.Icon = GetBitmapImage(GetImageStream(filename));
                Model.Pic1.SourceUrl = filename;
                return;
            }
            MessageBox.Show(Window, "Invalid fie type", "PSXPackager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Snd0_OnDrop(object sender, DragEventArgs e)
        {
            if (TryGetFilename(e.Data, new[] { ".at3" }, out var filename))
            {
                //_viewModel.Snd0.Icon = GetBitmapImage(GetImageStream(filename));
                Model.Snd0.SourceUrl = filename;

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
            saveFileDialog.Filter = "Supported files|*.bin;*.cue;*.iso;*.img|All files|*.*";
            var dlgResult = saveFileDialog.ShowDialog();

            if (dlgResult.GetValueOrDefault(false))
            {
                // clear the old TOC
                disc.SourceTOC = null;

                var imagePath = saveFileDialog.FileName;

                var isCue = false;

                if (Path.GetExtension(saveFileDialog.FileName).Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
                {
                    var sheet = CueFileReader.Read(saveFileDialog.FileName);
                    var firstEntry = sheet.FileEntries.First();
                    var fileDirectory = Path.GetDirectoryName(saveFileDialog.FileName);
                    imagePath = Path.Combine(fileDirectory, firstEntry.FileName);
                    disc.SourceTOC = saveFileDialog.FileName;
                    isCue = true;
                }
                else
                {
                    var cueFilename = Path.GetFileNameWithoutExtension(imagePath) + ".cue";
                    var dirPath = Path.GetDirectoryName(imagePath);
                    var cuePath = Path.Combine(dirPath, cueFilename);
                    if (File.Exists(cuePath))
                    {
                        var result = MessageBox.Show("A CUE file was found for the selected image, do you want to use it?", "Load ISO", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            disc.SourceTOC = Path.Combine(dirPath, cueFilename);
                            isCue = true;
                        }
                    }
                }

                uint fileSize = 0;


                if (isCue)
                {
                    var sourcePath = Path.GetDirectoryName(imagePath);

                    var sheet = CueFileReader.Read(disc.SourceTOC);
                    foreach (var fileEntry in sheet.FileEntries)
                    {
                        var binPath = Path.Combine(sourcePath, fileEntry.FileName);
                        var fileInfo = new FileInfo(binPath);
                        fileSize += (uint)fileInfo.Length;
                    }

                    disc.SourceUrl = saveFileDialog.FileName;
                }
                else
                {
                    var fileInfo = new FileInfo(imagePath);
                    fileSize = (uint)fileInfo.Length;
                    disc.SourceUrl = imagePath;
                }


                var gameId = GameDB.FindGameId(imagePath);

                if (gameId != null)
                {
                    disc.GameID = gameId;
                    var game = _gameDb.GetEntryByScannerID(gameId);
                    disc.Title = game.GameName;
                }
                else
                {
                    MessageBox.Show(Window, $"The GameID could not be detected. Please select the GameID manually",
                        "PSXPackager",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    var window = new GameListWindow();
                    window.Owner = Window;
                    var result = window.ShowDialog();

                    if (result is true)
                    {
                        gameId = window.SelectedGame.GameID;
                        disc.GameID = gameId;
                        disc.Title = window.SelectedGame.GameName;
                    }
                    else
                    {
                        gameId = "SCUS-00000";
                        disc.GameID = gameId;
                        disc.Title = "Untitled Game";
                    }
                }

                disc.Size = fileSize;
                disc.IsEmpty = false;
                disc.IsRemoveEnabled = true;
                disc.IsLoadEnabled = true;
                disc.IsSaveAsEnabled = false;
                disc.RemoveCommand = new RelayCommand((o) => Remove(disc));

                if (disc.Index == 0)
                {
                    Model.SFOEntries = new ObservableCollection<SFOEntry>()
                    {
                        new() { Key = SFOKeys.BOOTABLE, Value = 0x01, IsEditable = false },
                        new() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category, IsEditable = false  },
                        new() { Key = SFOKeys.DISC_ID, Value = gameId, IsEditable = true  },
                        new() { Key = SFOKeys.DISC_VERSION, Value =  "1.00", IsEditable = true  },
                        new() { Key = SFOKeys.LICENSE, Value =  SFOValues.License, IsEditable = true  },
                        new() { Key = SFOKeys.PARENTAL_LEVEL, Value =  0x01 , IsEditable = false },
                        new() { Key = SFOKeys.PSP_SYSTEM_VER, Value =  "3.01", IsEditable = true  },
                        new() { Key = SFOKeys.REGION, Value =  0x8000, IsEditable = true },
                        new() { Key = SFOKeys.TITLE, Value = disc.Title, IsEditable = true  },
                    };
                }


                _model.SelectedDisc = disc;

                Model.IsDirty = true;
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
                var sourceUrl = Model.Discs.Single(d => d.Index == context.Index).SourceUrl;

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
                                Model.Status = "Extracting disc image...";
                                Model.MaxProgress = disc.IsoSize;
                                Model.IsBusy = true;
                                _lastvalue = 0;
                                disc.ProgressEvent = ProgressEvent;
                                disc.CopyTo(output, _cancellationTokenSource.Token);

                                if (!_cancellationTokenSource.IsCancellationRequested)
                                {
                                    var cueFile = TOCHelper.TOCtoCUE(disc.TOC, Path.GetFileName(saveFileDialog.FileName));
                                    
                                    var cueFilename = Path.GetFileNameWithoutExtension(saveFileDialog.FileName) + ".cue";
                                    var dirPath = Path.GetDirectoryName(saveFileDialog.FileName);
                                    var cuePath = Path.Combine(dirPath, cueFilename);

                                    CueFileWriter.Write(cueFile, cuePath);

                                    Model.Status = "";
                                    Model.MaxProgress = 100;
                                    Model.Progress = 0;
                                    Model.IsBusy = false;
                                    Dispatcher.Invoke(() =>
                                    {
                                        MessageBox.Show(Window, $"Disc image has been extracted to \"{saveFileDialog.FileName}\"",
                                            "PSXPackager",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                    });
                                }
                                else
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        MessageBox.Show(Window, $"The operation was cancelled",
                                            "PSXPackager",
                                            MessageBoxButton.OK, MessageBoxImage.Information);
                                    });
                                }
                            }
                        }
                    });
                }
            }


        }

        private void ProgressEvent(uint progress)
        {
            var percent = progress / (float)Model.MaxProgress * 100f;
            if (percent - _lastvalue > 0.25)
            {
                Model.Progress = progress;
                Model.Status = $"Extracting disc image... ({percent:F0}%)";
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
                Model.IsDirty = true;
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
            Model.IsDirty = true;
        }

        public void NewPBP()
        {
            if (IsBusy)
            {
                var result = MessageBox.Show(Window, "An operation is in progress. Do you want to cancel?", "PSXPackager",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                    return;
            }

            if (Model.IsDirty)
            {
                var result = MessageBox.Show(Window, "You have unsaved changes. Are you sure you want to continue?", "PSXPackager",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result == MessageBoxResult.No)
                    return;
            }

            ResetModel();
        }

        public void SavePSP()
        {
            Save(true);
        }

        private void ResetSFO_Click(object sender, RoutedEventArgs e)
        {
            if (!Model.Discs[0].IsEmpty)
            {
                var gameId = Model.Discs[0].GameID;
                var game = _gameDb.GetEntryByScannerID(gameId);

                Model.SFOEntries = new ObservableCollection<SFOEntry>()
                {
                    new() { Key = SFOKeys.BOOTABLE, Value = 0x01, IsEditable = false },
                    new() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category, IsEditable = false  },
                    new() { Key = SFOKeys.DISC_ID, Value = gameId, IsEditable = true  },
                    new() { Key = SFOKeys.DISC_VERSION, Value =  "1.00", IsEditable = true  },
                    new() { Key = SFOKeys.LICENSE, Value =  SFOValues.License, IsEditable = true  },
                    new() { Key = SFOKeys.PARENTAL_LEVEL, Value =  0x01 , IsEditable = false },
                    new() { Key = SFOKeys.PSP_SYSTEM_VER, Value =  "3.01", IsEditable = true  },
                    new() { Key = SFOKeys.REGION, Value =  0x8000, IsEditable = true },
                    new() { Key = SFOKeys.TITLE, Value = game.GameName, IsEditable = true  },
                };
            }
            else
            {
                Model.SFOEntries = new ObservableCollection<SFOEntry>()
                {
                    new() { Key = SFOKeys.BOOTABLE, Value = 0x01, IsEditable = false },
                    new() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category, IsEditable = false  },
                    new() { Key = SFOKeys.DISC_ID, Value = "", IsEditable = true  },
                    new() { Key = SFOKeys.DISC_VERSION, Value =  "1.00", IsEditable = true  },
                    new() { Key = SFOKeys.LICENSE, Value =  SFOValues.License, IsEditable = true  },
                    new() { Key = SFOKeys.PARENTAL_LEVEL, Value =  0x01 , IsEditable = false },
                    new() { Key = SFOKeys.PSP_SYSTEM_VER, Value =  "3.01", IsEditable = true  },
                    new() { Key = SFOKeys.REGION, Value =  0x8000, IsEditable = true },
                    new() { Key = SFOKeys.TITLE, Value = "", IsEditable = true  },
                };
            }
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private void SelectGameID_Click(object sender, RoutedEventArgs e)
        {
            if (_model.SelectedDisc is { IsEmpty: false })
            {
                var window = new GameListWindow();
                window.Owner = Window;
                var result = window.ShowDialog();
                if (result is true)
                {
                    _model.SelectedDisc.GameID = window.SelectedGame.GameID;
                    _model.SelectedDisc.Title = window.SelectedGame.GameName;
                }
            }
          
        }
    }
}
