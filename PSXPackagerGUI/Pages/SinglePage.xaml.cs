using DiscUtils;
using DiscUtils.Raw;
using Popstation;
using Popstation.Database;
using Popstation.Pbp;
using PSXPackager.Audio;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using PSXPackager.Common.Notification;
using PSXPackagerGUI.Common;
using PSXPackagerGUI.Models;
using PSXPackagerGUI.Models.Resource;
using PSXPackagerGUI.Shaders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private readonly CDAudioPlayer _player;

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
            _player = new CDAudioPlayer();
            _player.Stopped += PlayerOnStopped;

            InitializeComponent();

            TextFormattingHelper.Visual = this;

            Model = new SingleModel
            {
                Icon0 = ResourceModel.ImageResource(ResourceType.ICON0, 80, 80),
                Icon1 = ResourceModel.OtherResource(ResourceType.ICON1),
                Pic0 = ResourceModel.ImageResource(ResourceType.PIC0, 310, 180),
                Pic1 = ResourceModel.ImageResource(ResourceType.PIC1, 480, 272),
                Snd0 = ResourceModel.OtherResource(ResourceType.SND0),
                Boot = ResourceModel.ImageResource(ResourceType.BOOT, 480, 272),
                IsDirty = false,
                MaxProgress = 100,
                Progress = 0,
                Settings = _settings
            };

            DataContext = Model;

            Model.PropertyChanged += ModelOnPropertyChanged;
            _settings.PropertyChanged += SettingsOnPropertyChanged;

            ResetModel();
            _stopwatch = Stopwatch.StartNew();
            CompositionTarget.Rendering += CompositionTargetOnRendering;

            timer = new Timer(Callback, null, new TimeSpan(0, 0, 1), new TimeSpan(0, 0, 1));
            Window.Closed += Window_Closed;

        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            timer?.Dispose();
        }

        private Timer timer;

        private void Callback(object? state)
        {
            _model.CurrentTime = $"{DateTime.Now:M/d h:mm tt}";
        }


        private Stopwatch _stopwatch;

        private void CompositionTargetOnRendering(object? sender, EventArgs e)
        {
            WavesEffect.Time = _stopwatch.Elapsed.TotalSeconds;
        }


        private void SettingsOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsModel.GenerateIconFrame))
            {
                if (_settings.GenerateIconFrame)
                {
                    var appPath = ApplicationInfo.AppPath;

                    var alphaMaskUri = Path.Combine(appPath, "Resources", "alpha.png");
                    var overlayUri = Path.Combine(appPath, "Resources", "overlay.png");

                    using var maskStream = new FileStream(alphaMaskUri, FileMode.Open, FileAccess.Read);
                    Model.Icon0.Composite.SetAplhaMask(ImageProcessing.GetBitmapImage(maskStream));

                    using var frameStream = new FileStream(overlayUri, FileMode.Open, FileAccess.Read);
                    Model.Icon0.Composite.AddLayer(new ImageLayer(ImageProcessing.GetBitmapImage(frameStream), "frame", overlayUri));

                    Model.Icon0.RefreshIcon();
                }
                else
                {
                    Model.Icon0.Composite.RemoveAplhaMask();
                    var layers = Model.Icon0.Composite.Layers.Where(d => d.Name == "frame").ToList();
                    foreach (var layer in layers)
                    {
                        Model.Icon0.Composite.Layers.Remove(layer);
                    }
                    Model.Icon0.RefreshIcon();
                }
            }
        }

        private void ModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SingleModel.SaveID):
                    Model.SFOEntries.FirstOrDefault(d => d.Key == SFOKeys.DISC_ID).Value = Model.SaveID;
                    break;
                case nameof(SingleModel.SaveTitle):
                    Model.SFOEntries.FirstOrDefault(d => d.Key == SFOKeys.TITLE).Value = Model.SaveTitle;
                    break;
            }
        }


        private void ResetModel()
        {
            Model.Icon0.Reset();
            Model.Icon1.Reset();
            Model.Pic0.Reset();
            Model.Pic1.Reset();
            Model.Snd0.Reset();
            Model.Boot.Reset();

            var result1 = ResourceHelper.LoadResource(Model.Icon0, GetDefaultResourceFile(Model.Icon0.Type), true);
            var result2 = ResourceHelper.LoadResource(Model.Pic1, GetDefaultResourceFile(Model.Pic1.Type), true);
            var result3 = ResourceHelper.LoadTemplate(Model.Pic0, GetDefaulTemplateFile(Model.Pic0.Type), true);


            Model.Icon0.Composite.Render();
            Model.Pic0.Composite.Render();
            Model.Pic1.Composite.Render();

            Model.Icon0.RefreshIcon();
            Model.Pic0.RefreshIcon();
            Model.Pic1.RefreshIcon();

            //ResourceHelper.LoadResource(Model.Pic1, GetDefaultResourceFile(Model.Pic1.Type), true);

            //LoadResource(Model.Boot, GetDefaultResourceFile(Model.Boot.Type));
            Model.Icon0.IsIncluded = true;
            Model.Pic0.IsIncluded = true;
            Model.Pic1.IsIncluded = true;

            Model.IsDirty = false;
            Model.MaxProgress = 100;
            Model.Progress = 0;
            Model.Discs = new ObservableCollection<Disc>(DummyDisc(0, 5));
            Model.IsNew = true;

            Model.SFOEntries = new ObservableCollection<SFOEntry>(GetDefaultSFOEntries());

            Model.SaveID = "";
            Model.SaveTitle = "";


            defaultSaveId = Model.SaveID;
            defaultSaveTitle = Model.SaveTitle;

            SetSFOMetaData();

            ResourceTabs.SelectedIndex = 0;
            Model.CurrentResourceName = "ICON0";
            Model.CurrentResource = Model.Icon0;

            result1.WarnIfErrors();
            result2.WarnIfErrors();
            result3.WarnIfErrors();
        }

        public void LoadPbp()
        {
            if (IsBusy)
            {
                MessageBox.Show(Window, "An operation is in progress. Please wait for the current operation to complete.", "PSXPackager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "PBP Files|*.pbp|All files|*.*";
            var result = openFileDialog.ShowDialog();

            if (result is not true)
            {
                return;
            }

            var path = openFileDialog.FileName;

            try
            {
                ResetModel();

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var pbpReader = new PbpReader(stream);

                    var discs = pbpReader.Discs.Select((d, i) =>
                    {
                        var game = _gameDb.GetEntryByGameID(d.DiscID);

                        var disc = new Disc()
                        {
                            Index = i,
                            GameID = d.DiscID,
                            Title = game.Title,
                            Size = d.IsoSize,
                            IsRemoveEnabled = true,
                            IsLoadEnabled = true,
                            IsSaveAsEnabled = true,
                            IsEmpty = false,
                            SourceUrl = $"pbp://{path}/disc{i}"
                        };

                        disc.RemoveCommand = new RelayCommand((o) => Remove(disc));

                        disc.Tracks = new ObservableCollection<Track>(TOCHelper.TOCtoCUE(d.TOC, disc.SourceUrl)
                            .FileEntries.SelectMany(d => d.Tracks).Select(d => new Track(d)));

                        return disc;
                    }).ToList();


                    var dummyDiscs = DummyDisc(discs.Count, 5 - discs.Count);

                    Model.Discs = new ObservableCollection<Disc>(discs.Concat(dummyDiscs));


                    void TryLoadResource(ResourceModel resource)
                    {
                        resource.IsIncluded = false;

                        if (resource.Type == ResourceType.BOOT)
                        {
                            if (pbpReader.TryGetBootImage(stream, out var bootStream))
                            {
                                resource.Composite.Clear();
                                resource.Composite.AddLayer(new ImageLayer(ImageProcessing.GetBitmapImage(bootStream), "image", $"pbp://{path}#{resource.Type}"));
                                resource.Composite.Render();
                                resource.RefreshIcon();
                                resource.SourceUrl = $"pbp://{path}#{resource.Type}";
                                resource.IsIncluded = true;
                                resource.HasResource = true;
                            }
                        }
                        else
                        {
                            if (pbpReader.TryGetResourceStream(resource.Type, stream, out var resourceStream))
                            {
                                //using var outStream = new FileStream(@"D:\roms\PSX\Animetic Story Game 1 - Card Captor Sakura (English v1.0)\test2.png", FileMode.Create, FileAccess.Write);
                                //resourceStream.CopyTo(outStream);
                                //outStream.Flush();
                                //resourceStream.Seek(0, SeekOrigin.Begin);
                                resource.Composite.Clear();
                                resource.Composite.AddLayer(new ImageLayer(ImageProcessing.GetBitmapImage(resourceStream), "image", $"pbp://{path}#{resource.Type}"));
                                resource.Composite.Render();
                                resource.RefreshIcon();
                                resource.SourceUrl = $"pbp://{path}#{resource.Type}";
                                resource.IsIncluded = true;
                                resource.HasResource = true;
                            }
                        }


                    }

                    TryLoadResource(Model.Icon0);
                    TryLoadResource(Model.Icon1);
                    TryLoadResource(Model.Pic0);
                    TryLoadResource(Model.Pic1);
                    TryLoadResource(Model.Snd0);
                    TryLoadResource(Model.Boot);

                    var entries = new List<SFOEntry>();
                    var keyHash = new HashSet<string>();

                    foreach (var sfoDataEntry in pbpReader.SFOData.Entries)
                    {
                        entries.Add(new SFOEntry()
                        {
                            Key = sfoDataEntry.Key,
                            Value = sfoDataEntry.Value,
                            IsEditable = GetIsEditable(sfoDataEntry.Key),
                            EntryType = GetEntryType(sfoDataEntry.Key),
                        });

                        switch (sfoDataEntry.Key)
                        {
                            case "DISC_ID":
                                Model.SaveID = (string)sfoDataEntry.Value;
                                break;
                            case "TITLE":
                                Model.SaveTitle = (string)sfoDataEntry.Value;
                                break;
                        }

                        keyHash.Add(sfoDataEntry.Key);
                    }


                    // Generate missing entries with empty values
                    var missingEntries = GetDefaultSFOEntries()
                        .Where(d => !keyHash.Contains(d.Key))
                        .Select(d =>
                        {
                            d.Value = "";
                            return d;
                        });

                    Model.SFOEntries = new ObservableCollection<SFOEntry>(entries.Concat(missingEntries).OrderBy(d => GetKeyOrder(d.Key)));


                    defaultSaveId = Model.SaveID;
                    defaultSaveTitle = Model.SaveTitle;

                    SetSFOMetaData();

                    Model.CurrentResource = Model.Icon0;
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
                SFOKeys.PARENTAL_LEVEL => true,
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

        private DiscInfo GetDiscInfo(Disc disc)
        {
            //var game = _gameDb.GetEntryByGameID(disc.GameID);
            return new DiscInfo()
            {
                GameID = disc.GameID,
                GameTitle = disc.Title,
                SourceIso = disc.SourceUrl,
                SourceToc = disc.SourceTOC,
            };
        }

        private Resource GetResourceOrEmpty(ResourceModel resource)
        {
            if (resource is { IsIncluded: true, HasResource: true })
            {
                return new Resource(resource.Type, resource.Stream, resource.Size);
            }

            return Resource.Empty(resource.Type);
        }

        string GetDefaultResourceFile(ResourceType type)
        {
            var ext = Popstation.Popstation.GetExtensionFromType(type);

            return Path.Combine(PSXPackager.Common.ApplicationInfo.AppPath, "Resources", $"{type}.{ext}");
        }


        string GetDefaulTemplateFile(ResourceType type)
        {
            return Path.Combine(PSXPackager.Common.ApplicationInfo.AppPath, "Templates", $"{type}.xml");
        }


        private Resource GetResourceOrDefault(ResourceModel resource)
        {
            if (resource is { Stream: not null, IsIncluded: true, HasResource: true })
            {
                return new Resource(resource.Type, resource.Stream, resource.Size);
            }

            var type = resource.Type;
            var defaultUrl = GetDefaultResourceFile(resource.Type);

            if (File.Exists(defaultUrl))
            {
                var fileName = defaultUrl;

                var info = new FileInfo(fileName);

                var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

                return new Resource(type, stream, (uint)info.Length);
            }

            return Resource.Empty(resource.Type);
        }

        private Window Window => _window;

        public bool IsBusy => Model.IsBusy;

        public SingleModel Model
        {
            get => _model;
            set => _model = value;
        }

        Regex gameIDregex = new Regex("(SCUS|SLUS|SLES|SCES|SCED|SLPS|SLPM|SCPS|SLED|SLPS|SIPS|ESPM|PBPX)(\\d{5})", RegexOptions.IgnoreCase);

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

            var discs = Model.Discs.Where(d => !d.IsEmpty).OrderBy(d => d.Index).ToList();

            foreach (var disc in discs)
            {
                if (!gameIDregex.IsMatch(disc.GameID))
                {
                    MessageBox.Show(Window, $"The GameID {disc.GameID} is not valid.", "PSXPackager",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            if (!gameIDregex.IsMatch(Model.SaveID))
            {
                MessageBox.Show(Window, $"The SaveID {Model.SaveID} is not valid.", "PSXPackager",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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

            string? filename = null;

            var gameId = Model.Discs.First().GameID;

            if (pspMode)
            {
                var ebootPath = Path.Combine(gameId, "EBOOT.PBP");

                MessageBox.Show(Window, $"Select the GAME folder to save {ebootPath}", "Save for PSP",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                var selectFolderDialog = new Microsoft.Win32.OpenFolderDialog();

                var dialogResult = selectFolderDialog.ShowDialog();

                if (dialogResult is true)
                {
                    filename = Path.Combine(selectFolderDialog.FolderName, ebootPath);

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
            }
            else
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
                saveFileDialog.AddExtension = true;
                saveFileDialog.DefaultExt = ".pbp";
                saveFileDialog.Filter = "EBOOT files|*.pbp|All files|*.*";
                var dialogResult = saveFileDialog.ShowDialog();
                if (dialogResult is true)
                {
                    filename = saveFileDialog.FileName;
                }
            }

            if (!string.IsNullOrEmpty(filename))
            {
                var disc1 = Model.Discs[0];
                var appPath = ApplicationInfo.AppPath;

                var options = new ConvertOptions()
                {
                    OutputPath = Path.GetDirectoryName(filename),
                    OriginalFilename = Path.GetFileName(filename),
                    DiscInfos = discs.Select(GetDiscInfo).ToList(),

                    DataPsp = GetResourceOrDefault(new ResourceModel() { Type = ResourceType.DATA }),

                    Icon0 = GetResourceOrDefault(Model.Icon0),
                    Pic1 = GetResourceOrDefault(Model.Pic1),
                    Pic0 = GetResourceOrDefault(Model.Pic0),
                    Boot = GetResourceOrEmpty(Model.Boot),

                    Snd0 = GetResourceOrEmpty(Model.Snd0),
                    Icon1 = GetResourceOrEmpty(Model.Icon1),

                    MainGameID = Model.SaveID,
                    MainGameTitle = Model.SaveTitle,
                    MainGameRegion = disc1.Region,

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
                        var (binfile, cuefile) = processing.PreProcessCue(discInfo.SourceIso, Path.GetTempPath());
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
            var selectedDisc = ((MenuItem)sender).DataContext as Disc;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = _settings.LastDiscImageDirectory;
            openFileDialog.Filter = "Supported files|*.bin;*.cue;*.img|All files|*.*";
            var dlgResult = openFileDialog.ShowDialog();

            if (dlgResult is true)
            {
                var fileDirectory = Path.GetDirectoryName(openFileDialog.FileName);

                _settings.LastDiscImageDirectory = fileDirectory;

				var discIndex = Model.Discs.IndexOf(selectedDisc); //Get index from selectedDisc

                var disc = new Disc()  //Create disc with correct index
				{
					Index = discIndex, //Index from the begining
					IsEmpty = false,
					IsLoadEnabled = true,
					IsSaveAsEnabled = false
				};

                // clear the old TOC
                disc!.SourceTOC = null;

                var imagePath = openFileDialog.FileName;

                var isCue = false;

                if (Path.GetExtension(openFileDialog.FileName).Equals(".cue", StringComparison.InvariantCultureIgnoreCase))
                {
                    var sheet = CueFileReader.Read(openFileDialog.FileName);
                    var firstEntry = sheet.FileEntries.First();

                    imagePath = Path.Combine(fileDirectory, firstEntry.FileName);
                    disc.SourceTOC = openFileDialog.FileName;
                    disc.Tracks = new ObservableCollection<Track>(sheet.FileEntries.SelectMany(d => d.Tracks).Select(d => new Track(d)));
                    isCue = true;
                }
                else
                {
                    var cueFilename = Path.GetFileNameWithoutExtension(imagePath) + ".cue";
                    var dirPath = Path.GetDirectoryName(imagePath)!;
                    var cuePath = Path.Combine(dirPath, cueFilename);
                    var generateCue = false;


                    if (File.Exists(cuePath))
                    {
                        var result = MessageBox.Show("A CUE file was found for the selected image, do you want to use it?", "Load ISO", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            var sheet = CueFileReader.Read(cuePath);
                            disc.SourceTOC = cuePath;
                            disc.Tracks = new ObservableCollection<Track>(sheet.FileEntries.SelectMany(d => d.Tracks).Select(d => new Track(d)));

                            isCue = true;
                        }
                        else
                        {
                            generateCue = true;
                        }
                    }
                    else
                    {
                        generateCue = true;
                    }

                    if (generateCue)
                    {
                        var sheet = CueFileReader.Dummy(imagePath);
                        disc.Tracks = new ObservableCollection<Track>(sheet.FileEntries.SelectMany(d => d.Tracks).Select(d => new Track(d)));
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

                    disc.SourceUrl = openFileDialog.FileName;
                }
                else
                {
                    var fileInfo = new FileInfo(imagePath);
                    fileSize = (uint)fileInfo.Length;
                    disc.SourceUrl = imagePath;
                }

                GameEntry game = null;
                try
                {
                    var gameId = GameDB.FindGameId(imagePath);

                    if (gameId != null)
                    {
                        game = _gameDb.GetEntryByGameID(gameId);
                    }
                    else
                    {
                        MessageBox.Show(Window, $"The GameID could not be detected. Please select the GameID manually",
                            "PSXPackager",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        var window = new GameListWindow();
                        window.Owner = Window;
                        var result = window.ShowDialog();

                        if (result is true && window.SelectedGame is { })
                        {
                            game = window.SelectedGame;
                        }
                        else
                        {
                            game = new GameEntry
                            {
                                SerialID = "SCUS-00000",
                                MainGameID = "SCUS00000",
                                Title = "Untitled Game",
                                MainGameTitle = "Untitled Game",
                                GameID = "SCUS00000",
                                Region = "NTSC"
                            };
                        }
                    }

                    disc.GameID = game.GameID;
                    disc.Title = game.Title;
                    Model.SaveID = game.MainGameID;
                    Model.SaveTitle = game.MainGameTitle;
                    disc.Region = game.Region;

                    defaultSaveId = Model.SaveID;
                    defaultSaveTitle = Model.SaveTitle;

                    if (disc.Index == 0)
                    {
                        Model.SFOEntries = new ObservableCollection<SFOEntry>()
                        {
                            new() { Key = SFOKeys.BOOTABLE, Value = 0x01, IsEditable = false },
                            new() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category, IsEditable = false  },
                            new() { Key = SFOKeys.DISC_ID, Value = game.MainGameID, IsEditable = true  },
                            new() { Key = SFOKeys.DISC_VERSION, Value =  "1.00", IsEditable = true  },
                            new() { Key = SFOKeys.LICENSE, Value =  SFOValues.License, IsEditable = true  },
                            new() { Key = SFOKeys.PARENTAL_LEVEL, Value =  SFOValues.ParentalLevel, IsEditable = true },
                            new() { Key = SFOKeys.PSP_SYSTEM_VER, Value =  SFOValues.PSPSystemVersion, IsEditable = true  },
                            new() { Key = SFOKeys.REGION, Value =  0x8000, IsEditable = true },
                            new() { Key = SFOKeys.TITLE, Value = game.MainGameTitle, IsEditable = true  },
                        };

                    }
                }
                catch (InvalidFileSystemException)
                {
                    MessageBox.Show(Window, "The disc does not appear to be a valid PlayStation disc",
                        "PSXPackager",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                disc.Size = fileSize;
                disc.IsEmpty = false;
                disc.IsRemoveEnabled = true;
                disc.IsLoadEnabled = true;
                disc.IsSaveAsEnabled = false;
                disc.RemoveCommand = new RelayCommand((o) => Remove(disc));

                _model.SelectedDisc = disc;

                Model.IsDirty = true;

                Model.Discs[discIndex] = disc;

                Model.SelectedDisc = disc;
            }

        }

        private void SaveImage_OnClick(object sender, RoutedEventArgs e)
        {
            var context = ((MenuItem)sender).DataContext as Disc;
            var pbpRegex = new Regex("pbp://(?<pbp>.*\\.pbp)/disc(?<disc>\\d)", RegexOptions.IgnoreCase);

            var game = _gameDb.GetEntryByGameID(context.GameID);


            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.FileName = $"{game.Title}.bin";
            saveFileDialog.Filter = "BIN files|*.bin|All files|*.*";
            saveFileDialog.DefaultExt = ".bin";
            saveFileDialog.AddExtension = true;
            var result = saveFileDialog.ShowDialog(Window);

            if (result is true)
            {
                var sourceUrl = Model.Discs.Single(d => d.Index == context.Index).SourceUrl;

                var match = pbpRegex.Match(sourceUrl);

                if (match.Success)
                {
                    var discIndex = int.Parse(match.Groups["disc"].Value);
                    var pbpPath = match.Groups["pbp"].Value;

                    Task.Run(() =>
                    {
                        using (var stream = new FileStream(pbpPath, FileMode.Open, FileAccess.Read))
                        {
                            var pbpReader = new PbpReader(stream);
                            var disc = pbpReader.Discs[discIndex];

                            using (var output = new FileStream(saveFileDialog.FileName, FileMode.Create,
                                       FileAccess.Write))
                            {
                                Model.Status = "Extracting disc image...";
                                Model.MaxProgress = disc.IsoSize;
                                Model.IsBusy = true;
                                _lastvalue = 0;
                                disc.ProgressEvent = ProgressEvent;

                                //var sourceStream = disc.GetDiscStream();
                                //var buffer = new byte[4096];
                                //int bytesRead = 0;
                                //int totalRead = 0;

                                //while ((bytesRead = sourceStream.Read(buffer, 0, 4096)) > 0)
                                //{
                                //    totalRead += bytesRead;
                                //    output.Write(buffer, 0, bytesRead);
                                //    Dispatcher.Invoke(() =>
                                //    {
                                //        Model.Status = $"{totalRead}";
                                //    });
                                //}
                                //output.Flush();

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
            Model.SFOEntries = new ObservableCollection<SFOEntry>()
            {
                new() { Key = SFOKeys.BOOTABLE, Value = 0x01, IsEditable = false },
                new() { Key = SFOKeys.CATEGORY, Value = SFOValues.PS1Category, IsEditable = false  },
                new() { Key = SFOKeys.DISC_ID, Value = defaultSaveId, IsEditable = true  },
                new() { Key = SFOKeys.DISC_VERSION, Value =  "1.00", IsEditable = true  },
                new() { Key = SFOKeys.LICENSE, Value =  SFOValues.License, IsEditable = true  },
                new() { Key = SFOKeys.PARENTAL_LEVEL, Value = SFOValues.ParentalLevel, IsEditable = true },
                new() { Key = SFOKeys.PSP_SYSTEM_VER, Value = SFOValues.PSPSystemVersion, IsEditable = true  },
                new() { Key = SFOKeys.REGION, Value =  0x8000, IsEditable = true },
                new() { Key = SFOKeys.TITLE, Value = defaultSaveTitle, IsEditable = true  },
            };
        }

        private string defaultSaveId;
        private string defaultSaveTitle;

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
                if (result is true && window.SelectedGame is { })
                {
                    _model.SelectedDisc.GameID = window.SelectedGame.GameID;
                    _model.SelectedDisc.Title = window.SelectedGame.Title;
                }
            }

        }


        private void Track_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (((ListViewItem)sender).DataContext is Track { DataType: "AUDIO" } track)
            {
                Play(track);

                e.Handled = true;
            }
        }

        private void PlayControls_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (((Image)sender).DataContext is Track { DataType: "AUDIO" } track)
            {
                Play(track);

                e.Handled = true;
            }
        }


        private void Play(Track track)
        {
            if (_model.SelectedTrack != null && _model.SelectedTrack != track)
            {
                _model.SelectedTrack.Status = TrackStatus.Stopped;
                _model.SelectedTrack.IsSelected = false;
            }

            switch (track.Status)
            {
                case TrackStatus.Stopped:
                    _player.PlayCueTrack(track.CueTrack);
                    _model.SelectedTrack = track;
                    track.IsSelected = true;
                    track.Status = TrackStatus.Playing;
                    break;
                case TrackStatus.Playing:
                    _player.Stop();
                    track.Status = TrackStatus.Stopped;
                    break;
            }
        }


        private void PlayerOnStopped(object? sender, CDAudioPlayerStopped e)
        {
            if (e.Track is not null)
            {
            }
        }

        private void SelectResource_Click(object sender, RoutedEventArgs e)
        {
            var resourceId = (string)((Control)sender).Tag;

            switch (resourceId)
            {
                case "ICON0":
                    Model.CurrentResource = Model.Icon0;
                    break;
                case "ICON1":
                    Model.CurrentResource = Model.Icon1;
                    break;
                case "PIC0":
                    Model.CurrentResource = Model.Pic0;
                    break;
                case "PIC1":
                    Model.CurrentResource = Model.Pic1;
                    break;
                case "SND0":
                    Model.CurrentResource = Model.Snd0;
                    break;
                case "BOOT":
                    Model.CurrentResource = Model.Boot;
                    break;
            }

            Model.CurrentResourceName = resourceId;
        }

        private void SelectSaveID_Click(object sender, RoutedEventArgs e)
        {
            if (_model.SelectedDisc is { IsEmpty: false })
            {
                var window = new GameListWindow();
                window.Owner = Window;
                var result = window.ShowDialog();
                if (result is true && window.SelectedGame is { })
                {
                    _model.SaveID = window.SelectedGame.MainGameID;
                    _model.SaveTitle = window.SelectedGame.MainGameTitle;
                }
            }
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems[0] is TabItem tabItem)
            {
                var resourceId = (string)tabItem.Tag;

                switch (resourceId)
                {
                    case "ICON0":
                        Model.CurrentResource = Model.Icon0;
                        break;
                    case "ICON1":
                        Model.CurrentResource = Model.Icon1;
                        break;
                    case "PIC0":
                        Model.CurrentResource = Model.Pic0;
                        break;
                    case "PIC1":
                        Model.CurrentResource = Model.Pic1;
                        break;
                    case "SND0":
                        Model.CurrentResource = Model.Snd0;
                        break;
                    case "BOOT":
                        Model.CurrentResource = Model.Boot;
                        break;
                }

                Model.CurrentResourceName = resourceId;
            }
        }

        private void SFOValue_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is SFOEntry entry)
            {
                switch (entry.Key)
                {
                    case "DISC_ID":
                        Model.SaveID = (string)entry.Value;
                        break;
                    case "TITLE":
                        Model.SaveTitle = (string)entry.Value;
                        break;
                }
            }
        }
    }
}
