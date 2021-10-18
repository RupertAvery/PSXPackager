using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using DiscUtils.Iso9660;
using Popstation;
using Popstation.Cue;
using Popstation.M3u;
using Popstation.Pbp;
using SevenZip;

namespace PSXPackager
{
    public class Processing
    {
        private readonly INotifier _notifier;
        private readonly IEventHandler _eventHandler;
        private readonly GameDB _gameDb = new GameDB(Path.Combine(ApplicationInfo.AppPath, "Resources", "gameInfo.db"));

        private List<string> tempFiles = new List<string>();

        public Processing(INotifier notifier, IEventHandler eventHandler)
        {
            _notifier = notifier;
            _eventHandler = eventHandler;
        }

        public bool ProcessFile(
            string file,
            ProcessOptions options,
            CancellationToken cancellationToken)
        {
            bool result = true;


            if (_eventHandler.Cancelled) return false;

            options.CheckIfFileExists = !_eventHandler.OverwriteIfExists && options.CheckIfFileExists;

            tempFiles.Clear();

            try
            {
                if (FileExtensionHelper.IsArchive(file))
                {
                    Unpack(file, options.TempPath, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return false;

                    file = "";

                    if (tempFiles.Count(FileExtensionHelper.IsImageFile) == 0)
                    {
                        _notifier?.Notify(PopstationEventEnum.Error, "No image files found!");
                        return false;
                    }
                    else if (tempFiles.Count(FileExtensionHelper.IsImageFile) == 1)
                    {
                        var cue = tempFiles.FirstOrDefault(FileExtensionHelper.IsCue);
                        if (cue != null)
                        {
                            file = cue;
                        }
                        else
                        {
                            file = tempFiles.FirstOrDefault(FileExtensionHelper.IsImageFile);
                        }
                    }
                    else if (tempFiles.Count(FileExtensionHelper.IsBin) > 1)
                    {
                        _notifier?.Notify(PopstationEventEnum.Info, $"Multi-bin image was found!");

                        var cue = tempFiles.FirstOrDefault(FileExtensionHelper.IsCue);
                        if (cue != null)
                        {
                            file = cue;
                        }
                        else
                        {
                            _notifier?.Notify(PopstationEventEnum.Warning, $"No cue sheet found!");
                            return false;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(file))
                {

                    if (FileExtensionHelper.IsPbp(file))
                    {
                        ExtractPbp(file, options, cancellationToken);
                    }
                    else
                    {
                        if (FileExtensionHelper.IsCue(file))
                        {
                            var (outfile, srcToc) = ProcessCue(file, options.TempPath);
                            result = ConvertIso(outfile, srcToc, options, cancellationToken);
                        }
                        else if (FileExtensionHelper.IsM3u(file))
                        {
                            var filePath = Path.GetDirectoryName(file);
                            var files = new List<string>();
                            var tocs = new List<string>();
                            var m3UFile = M3uFileReader.Read(file);

                            if (m3UFile.FileEntries.Count == 0)
                            {
                                _notifier?.Notify(PopstationEventEnum.Error, $"Invalid number of entries, found {m3UFile.FileEntries.Count}");
                                return false;
                            }
                            else if (m3UFile.FileEntries.Count > 5)
                            {
                                _notifier?.Notify(PopstationEventEnum.Error, $"Invalid number of entries, found {m3UFile.FileEntries.Count}, max is 5");
                                return false;
                            }

                            _notifier?.Notify(PopstationEventEnum.Info, $"Found {m3UFile.FileEntries.Count} entries");

                            foreach (var fileEntry in m3UFile.FileEntries)
                            {
                                if (FileExtensionHelper.IsCue(fileEntry))
                                {
                                    var (outfile, srcToc) = ProcessCue(Path.Combine(filePath, fileEntry), options.TempPath);
                                    files.Add(outfile);
                                    tocs.Add(srcToc);
                                }
                                else if (FileExtensionHelper.IsImageFile(fileEntry))
                                {
                                    files.Add(Path.Combine(filePath, fileEntry));
                                }
                                else
                                {
                                    _notifier?.Notify(PopstationEventEnum.Warning, $"Unsupported playlist entry '{fileEntry}'");
                                    _notifier?.Notify(PopstationEventEnum.Warning, "Only the following are supported: .cue .img .bin .iso");
                                    return false;
                                }
                            }
                            result = ConvertIsos(files.ToArray(), tocs.ToArray(), options, cancellationToken);
                        }
                        else
                        {
                            result = ConvertIso(file, "", options, cancellationToken);
                        }

                    }


                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _notifier?.Notify(PopstationEventEnum.Warning, "Conversion cancelled");
                    return false;
                }

            }
            catch (CancellationException ex)
            {
                _notifier?.Notify(PopstationEventEnum.Error, ex.Message);
                return false;
            }
            catch (FileNotFoundException ex)
            {
                _notifier?.Notify(PopstationEventEnum.Error, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _notifier?.Notify(PopstationEventEnum.Error, ex.Message);
                return false;
                //throw;
            }
            finally
            {
                if (tempFiles != null)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                }
            }

            return result;
        }

        private (string, string) ProcessCue(string file, string tempPath)
        {
            var filePath = Path.GetDirectoryName(file);

            var cueFiles = CueFileReader.Read(file);
            string srcToc = null;
            if (cueFiles.FileEntries.Count() > 1)
            {
                _notifier?.Notify(PopstationEventEnum.Info, $"Merging .bins...");
                var mergedBin = MergeBins(file, cueFiles, tempPath);
                var cueFile = Path.Combine(tempPath,
                    Path.GetFileNameWithoutExtension(mergedBin.Path) + ".cue");
                CueFileWriter.Write(mergedBin.CueFile, cueFile);
                srcToc = cueFile;
                file = mergedBin.Path;

                tempFiles.Add(mergedBin.Path);
                tempFiles.Add(cueFile);
            }
            else
            {
                srcToc = file;
                file = Path.Combine(filePath, cueFiles.FileEntries.First().FileName);
            }

            return (file, srcToc);
        }

        static MergedBin MergeBins(string file, CueFile cueFilex, string tempPath)
        {
            var mergedBin = new MergedBin();
            mergedBin.CueFile = new CueFile();

            var cueFilePath = Path.GetDirectoryName(file);


            long currentFrame = 0;

            var mergedFilename = Path.GetFileNameWithoutExtension(file) + " - MERGED.bin";

            mergedBin.Path = Path.Combine(tempPath, mergedFilename);

            var mcueFile = new CueFileEntry()
            {
                FileName = mergedFilename,
                FileType = "BINARY",
                Tracks = new List<CueTrack>()
            };

            mergedBin.CueFile.FileEntries.Add(mcueFile);

            using (var joinedFile = new FileStream(mergedBin.Path, FileMode.Create))
            {
                foreach (var cueFileEntry in cueFilex.FileEntries)
                {
                    var binPath = cueFileEntry.FileName;
                    if (Path.GetDirectoryName(binPath) == "" || Path.GetDirectoryName(binPath).StartsWith("..") || Path.GetDirectoryName(binPath).StartsWith("."))
                    {
                        binPath = Path.Combine(cueFilePath, cueFileEntry.FileName);
                    }

                    using (var srcStream = new FileStream(binPath, FileMode.Open))
                    {
                        srcStream.CopyTo(joinedFile);

                        foreach (var item in cueFileEntry.Tracks)
                        {
                            var indexes = new List<CueIndex>();
                            foreach (var idx in item.Indexes)
                            {
                                var newIndex = new CueIndex
                                {
                                    Number = idx.Number,
                                    Position = idx.Position + TOCHelper.PositionFromFrames(currentFrame)
                                };
                                indexes.Add(newIndex);
                            }
                            var newTrack = new CueTrack()
                            {
                                DataType = item.DataType,
                                Number = item.Number,
                                Indexes = indexes
                            };
                            mcueFile.Tracks.Add(newTrack);
                        }

                        var frames = srcStream.Length / 2352;
                        currentFrame += frames;
                    }

                }
            }

            return mergedBin;
        }

        static string Shorten(ulong size)
        {
            switch (size)
            {
                case var _ when size >= 1048576:
                    return $"{size / 1048576}MB";
                case var _ when size > 1024:
                    return $"{size / 1024}KB";
                default:
                    return $"{size}B";
            }
        }

        void Unpack(string file, string tempPath, CancellationToken cancellationToken)
        {
            List<string> files;

            using (var archiveFile = new SevenZipExtractor(file))
            {
                var archiveFiles = archiveFile.ArchiveFileData.Select(x => x.FileName).ToList();

                archiveFile.FileExtractionStarted += (sender, args) => _notifier.Notify(PopstationEventEnum.DecompressStart, args.FileInfo.FileName);
                archiveFile.Extracting += ArchiveFileOnExtracting;
                archiveFile.FileExtractionFinished += (sender, args) => _notifier.Notify(PopstationEventEnum.DecompressComplete, null);
                //archiveFile.BeginExtractFiles(ExtractFileCallback);
                //var unpackTasks = new List<Task>();
                archiveFile.ExtractFiles(tempPath, archiveFiles.ToArray());

                files = archiveFiles.Select(x => Path.Combine(tempPath, x)).ToList();

                //foreach (var entry in archiveFile.ArchiveFileData)
                //{
                //    if (FileExtensionHelper.IsImageFile(entry.FileName) || FileExtensionHelper.IsCue(entry.FileName))
                //    {
                //        //Console.WriteLine($"Extracting {entry.FileName} ({Shorten(entry.Size)})");
                //        var path = Path.Combine(tempPath, entry.FileName);
                //        // extract to file
                //        files.Add(path);

                //        _notifier.Notify(PopstationEventEnum.DecompressStart, entry.FileName);
                //        _notifier.Notify(PopstationEventEnum.DecompressComplete, null);

                //        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
                //        {
                //            _notifier.Notify(PopstationEventEnum.DecompressStart, entry.FileName);
                //            archiveFile.ExtractFile(entry.FileName, stream);
                //            _notifier.Notify(PopstationEventEnum.DecompressComplete, null);
                //        }

                //    }
                //    if (cancellationToken.IsCancellationRequested)
                //    {
                //        return files;
                //    }
                //}

                //Task.WaitAll(unpackTasks.ToArray());
                archiveFile.Extracting -= ArchiveFileOnExtracting;

            }

            tempFiles.AddRange(files);
        }

        //private void ExtractFileCallback(ExtractFileCallbackArgs extractfilecallbackargs)
        //{
        //    extractfilecallbackargs.CancelExtraction
        //}

        private void ArchiveFileOnExtracting(object sender, ProgressEventArgs e)
        {
            _notifier.Notify(PopstationEventEnum.DecompressProgress, e.PercentDone);
        }

        static string FindGameId(string srcIso)
        {
            var regex = new Regex("(SCUS|SLUS|SLES|SCES|SCED|SLPS|SLPM|SCPS|SLED|SLPS|SIPS|ESPM|PBPX)[_-](\\d{3})\\.(\\d{2})", RegexOptions.IgnoreCase);
            var bootRegex = new Regex("BOOT\\s*=\\s*cdrom:\\\\?(?:.*?\\\\)?(.*?);1");

            using (var stream = new FileStream(srcIso, FileMode.Open))
            {
                var cdReader = new CDReader(stream, false, 2352);

                // Why doesn't a root file check not work?
                //foreach (var file in cdReader.GetFiles("\\"))
                //{
                //    var filename = file.Substring(1, file.LastIndexOf(";"));
                //    var match = regex.Match(filename);
                //    if (match.Success)
                //    {
                //        gameId = $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}";
                //        break;
                //    }
                //}
                var syscnfFound = false;

                foreach (var file in cdReader.GetFiles("\\"))
                {
                    var filename = file.Substring(1, file.LastIndexOf(";") - 1);
                    if (filename != "SYSTEM.CNF") continue;

                    syscnfFound = true;

                    using (var datastream = cdReader.OpenFile(file, FileMode.Open))
                    {
                        datastream.Seek(24, SeekOrigin.Begin);
                        var textReader = new StreamReader(datastream);
                        var bootLine = textReader.ReadLine();
                        var bootmatch = bootRegex.Match(bootLine);
                        if (!bootmatch.Success) continue;

                        var match = regex.Match(bootmatch.Groups[1].Value);
                        if (match.Success)
                        {
                            return $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}";
                        }
                    }
                }
            }

            return null;
        }

        private GameEntry GetDummyGame(string gameId, string gameTitle)
        {
            return new GameEntry()
            {
                GameID = gameId,
                ScannerID = gameId,
                GameName = gameTitle,
                SaveDescription = gameTitle,
                Format = "NTSC",
                SaveFolderName = gameId.Replace("-", "")
            };
        }

        private GameEntry GetGameEntry(string gameId, string path, bool showMessages = true)
        {
            GameEntry game;
            var dummyTitle = Path.GetFileNameWithoutExtension(path);

            if (gameId != null)
            {
                game = _gameDb.GetEntryByScannerID(gameId.ToUpper());
                if (game == null)
                {
                    if (showMessages)
                        _notifier?.Notify(PopstationEventEnum.Warning, $"Did not find a Game with ID {gameId} Using title {dummyTitle}");

                    game = GetDummyGame(gameId, dummyTitle);
                }

                if (showMessages)
                    _notifier?.Notify(PopstationEventEnum.Info, $"Found {gameId} \"{game.GameName}\"");
            }
            else
            {
                if (showMessages)
                    _notifier?.Notify(PopstationEventEnum.Warning, "Did not find a Game ID! Using SLUS-00000");

                game = GetDummyGame("SLUS-00000", dummyTitle);
            }

            return game;
        }

        private bool ConvertIsos(
            string[] srcIsos,
            string[] srcTocs,
            ProcessOptions processOptions,
            CancellationToken cancellationToken)
        {
            var appPath = ApplicationInfo.AppPath;
            var gameId = FindGameId(srcIsos[0]);
            var game = GetGameEntry(gameId, srcIsos[0], false);

            if (!string.IsNullOrEmpty(processOptions.GenerateResourceFolders))
            {
                var path = GetResouceFolderPath(processOptions, processOptions.GenerateResourceFolders, game, srcIsos[0], true);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return true;
            }

            var options = new ConvertOptions()
            {

                DestinationPbp = Path.Combine(processOptions.OutputPath, $"{game.SaveDescription}.PBP"),
                DiscInfos = new List<DiscInfo>(),
                MainGameTitle = game.SaveDescription,
                MainGameID = game.SaveFolderName,
                MainGameRegion = game.Format,
                SaveTitle = game.SaveDescription,
                SaveID = game.SaveFolderName,
                BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                CompressionLevel = processOptions.CompressionLevel,
                CheckIfFileExists = processOptions.CheckIfFileExists,
                SkipIfFileExists = processOptions.SkipIfFileExists,
                FileNameFormat = processOptions.FileNameFormat,
            };

            SetResources(processOptions, options, game, srcIsos[0]);

            for (var i = 0; i < srcIsos.Length; i++)
            {
                gameId = FindGameId(srcIsos[i]);
                game = GetGameEntry(gameId, srcIsos[i]);

                options.DiscInfos.Add(new DiscInfo()
                {
                    GameID = game.ScannerID,
                    GameTitle = game.SaveDescription,
                    GameName = game.GameName,
                    Region = game.Format,
                    MainGameID = game.SaveFolderName,
                    SourceIso = srcIsos[i],
                    SourceToc = i < srcTocs.Length ? srcTocs[i] : "",
                });
            }

            _notifier?.Notify(PopstationEventEnum.Info, $"Using Title '{game.SaveDescription}'");

            var popstation = new Popstation.Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            return popstation.Convert(options, cancellationToken);
        }

        private string GetResouceFolderPath(ProcessOptions processOptions, string mode, GameEntry entry, string srcIso, bool forGenerate = false)
        {
            string path;
            
            if (!string.IsNullOrEmpty(mode))
            {
                var filename = Path.GetFileNameWithoutExtension(srcIso);
                path = Path.GetDirectoryName(srcIso);

                if (!string.IsNullOrEmpty(processOptions.ResourceFoldersPath))
                {
                    path = processOptions.ResourceFoldersPath;
                }

                switch (mode)
                {
                    case "gameid":
                        path = Path.Combine(path, entry.GameID);
                        break;
                    case "title":
                        path = Path.Combine(path, entry.GameName);
                        break;
                    case "filename":
                        path = Path.Combine(path, filename);
                        break;
                    default:
                        path = Path.Combine(path, filename);
                        break;
                }

            }
            else
            {
                if (forGenerate)
                {
                    path = Path.GetDirectoryName(srcIso);
                }
                else
                {
                    var appPath = ApplicationInfo.AppPath;
                    var defaultPath = Path.Combine(appPath, "Resources");
                    path = defaultPath;
                }
            }

            return path;
        }


        private void SetResources(ProcessOptions processOptions, ConvertOptions options, GameEntry entry, string srcIso)
        {
            var appPath = ApplicationInfo.AppPath;
            var defaultPath = Path.Combine(appPath, "Resources");

            var path = GetResouceFolderPath(processOptions, processOptions.ImportResources, entry, srcIso);

            Resource GetResourceOrDefault(ResourceType type, string filename)
            {
                var resourcePath =  Path.Combine(path, filename);
                if (!File.Exists(resourcePath))
                {
                    resourcePath = Path.Combine(defaultPath, filename);
                }
                return new Resource(type, resourcePath);
            }

            options.Icon0 = GetResourceOrDefault(ResourceType.ICON0, "ICON0.PNG");
            options.Icon1 = GetResourceOrDefault(ResourceType.ICON1, "ICON1.PMF");
            options.Pic0 = GetResourceOrDefault(ResourceType.PIC0, "PIC0.PNG");
            options.Pic1 = GetResourceOrDefault(ResourceType.PIC1, "PIC1.PNG");
            options.Snd0 = GetResourceOrDefault(ResourceType.SND0, "SND0.AT3");
        }

        private bool ConvertIso(
            string srcIso,
            string srcToc,
            ProcessOptions processOptions,
            CancellationToken cancellationToken)
        {
            var appPath = ApplicationInfo.AppPath;
            var gameId = FindGameId(srcIso);
            var game = GetGameEntry(gameId, srcIso, false);

            if (!string.IsNullOrEmpty(processOptions.GenerateResourceFolders))
            {
                var path = GetResouceFolderPath(processOptions, processOptions.GenerateResourceFolders, game, srcIso, true);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return true;
            }

            var options = new ConvertOptions()
            {
                DiscInfos = new List<DiscInfo>()
                {
                    new DiscInfo()
                    {
                        GameID = game.ScannerID,
                        GameTitle = game.SaveDescription,
                        GameName = game.GameName,
                        Region = game.Format,
                        MainGameID = game.SaveFolderName,
                        SourceIso = srcIso,
                        SourceToc = srcToc,
                    }
                },
                DestinationPbp = Path.Combine(processOptions.OutputPath, $"{game.GameName}.PBP"),
                MainGameTitle = game.GameName,
                MainGameRegion = game.Format,
                MainGameID = game.SaveFolderName,
                SaveTitle = game.SaveDescription,
                SaveID = game.SaveFolderName,
                BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                CompressionLevel = processOptions.CompressionLevel,
                CheckIfFileExists = processOptions.CheckIfFileExists,
                SkipIfFileExists = processOptions.SkipIfFileExists,
                FileNameFormat = processOptions.FileNameFormat,
            };

            SetResources(processOptions, options, game, srcIso);

            _notifier.Notify(PopstationEventEnum.Info, $"Using Title '{game.GameName}'");

            var popstation = new Popstation.Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            return popstation.Convert(options, cancellationToken);
        }

        private void ExtractPbp(string srcPbp,
            ProcessOptions processOptions,
            CancellationToken cancellationToken)
        {
            var info = new ExtractOptions()
            {
                SourcePbp = srcPbp,
                OutputPath = processOptions.OutputPath,
                DiscName = "- Disc {0}",
                Discs = processOptions.Discs,
                CreateCuesheet = true,
                CheckIfFileExists = processOptions.CheckIfFileExists,
                SkipIfFileExists = processOptions.SkipIfFileExists,
                FileNameFormat = processOptions.FileNameFormat,
                ExtractResources = processOptions.ExtractResources,
                GenerateResourceFolders = processOptions.GenerateResourceFolders,
                ResourceFoldersPath = processOptions.ResourceFoldersPath,
                GetGameInfo = (gameId) =>
                {
                    var game = GetGameEntry(gameId, srcPbp, false);
                    if (game == null)
                    {
                        return null;
                    }
                    return new GameInfo()
                    {
                        GameID = game.ScannerID,
                        GameName = game.GameName,
                        Title = game.SaveDescription,
                        MainGameID = game.SaveFolderName,
                        Region = game.Format
                    };
                }
            };

            var popstation = new Popstation.Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            popstation.Extract(info, cancellationToken);
        }
    }
}