using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Popstation.Database;
using Popstation.M3u;
using Popstation.Pbp;
using PSXPackager.Common;
using PSXPackager.Common.Cue;
using PSXPackager.Common.Notification;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace Popstation
{
    public class Processing
    {
        private readonly INotifier _notifier;
        private readonly IEventHandler _eventHandler;
        private readonly GameDB _gameDb;
        private long _currentFileDecompressedSize;

        private List<string> tempFiles = new List<string>();

        public Processing(INotifier notifier, IEventHandler eventHandler, GameDB gameDb)
        {
            _notifier = notifier;
            _eventHandler = eventHandler;
            _gameDb = gameDb;
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
                var originalFile = file;

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
                        if (options.ExtractResources)
                        {
                            _notifier?.Notify(PopstationEventEnum.Error, "Input file for Resource Extract must be .PBP");
                            return false;
                        }

                        if (FileExtensionHelper.IsCue(file))
                        {
                            var (outfile, srcToc) = PreProcessCue(file, options.TempPath);

                            result = ConvertIso(originalFile, outfile, srcToc, options, cancellationToken);
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
                                    var (outfile, srcToc) = PreProcessCue(Path.Combine(filePath, fileEntry), options.TempPath);
                                    files.Add(outfile);
                                    tocs.Add(srcToc);
                                }
                                else if (FileExtensionHelper.IsImageFile(fileEntry))
                                {
                                    files.Add(Path.Combine(filePath, fileEntry));
                                }
                                else
                                {
                                    _notifier?.Notify(PopstationEventEnum.Error, $"Unsupported playlist entry '{fileEntry}'");
                                    _notifier?.Notify(PopstationEventEnum.Error, "Only the following are supported: .cue .img .bin .iso");
                                    return false;
                                }
                            }
                            result = ConvertIsos(originalFile, files.ToArray(), tocs.ToArray(), options, cancellationToken);
                        }
                        else
                        {
                            result = ConvertIso(originalFile, file, "", options, cancellationToken);
                        }

                    }


                }

                if (cancellationToken.IsCancellationRequested)
                {
                    _notifier?.Notify(PopstationEventEnum.Warning, "Conversion cancelled");
                    _notifier?.Notify(PopstationEventEnum.Cancelled, null);
                    return false;
                }

                _notifier?.Notify(PopstationEventEnum.ConvertComplete, null);

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
                Cleanup();
            }

            return result;
        }

        public void Cleanup()
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

        /// <summary>
        /// Pre-processes a cue sheet. If multiple .bin files are referenced, merges them into a single .bin file and generates a new cue sheet.
        /// </summary>
        /// <param name="cueFilePath"></param>
        /// <param name="tempPath"></param>
        /// <returns></returns>
        public (string, string) PreProcessCue(string cueFilePath, string tempPath)
        {
            var cueFile = CueFileReader.Read(cueFilePath);

            // If there are multiple .bin files, merge them into a single .bin
            if (cueFile.FileEntries.Count > 1)
            {
                _notifier?.Notify(PopstationEventEnum.Info, $"Merging .bins...");

                var fileName = Path.GetFileNameWithoutExtension(cueFilePath);
                var mergedBinFileName = fileName + "_merged.bin";
                var mergedBinFilePath = Path.Combine(tempPath, mergedBinFileName);

                var mergedCueFileName = fileName + "_merged.cue";
                var mergedCueFilePath = Path.Combine(tempPath, mergedCueFileName);

                using var outputStream = new FileStream(mergedBinFilePath, FileMode.Create, FileAccess.Write);

                var mergedCueFile = MergeBins(outputStream, mergedBinFileName, cueFile);
                
                CueFileWriter.Write(mergedCueFile, mergedCueFilePath);

                tempFiles.Add(mergedBinFilePath);
                tempFiles.Add(mergedCueFilePath);

                return (mergedBinFilePath, mergedCueFilePath);
            }
            else
            {
                var cueDirectory = Path.GetDirectoryName(cueFilePath);
                var binFileName = cueFile.FileEntries[0].FileName;
                if (!Path.IsPathFullyQualified(binFileName))
                {
                    binFileName = Path.Combine(cueDirectory, binFileName);
                }

                return (binFileName, cueFilePath);
            }
        }


        /// <summary>
        /// Merges multiple .bin files referenced in a cue sheet into a single .bin file
        /// </summary>
        /// <param name="outputStream">The stream where the merged bin will be written to</param>
        /// <param name="binFileName">The name of the file that will be generated and placed in the singe FILE entry</param>
        /// <param name="unmergedCue">The <see cref="CueFile"/> containing the bins to be merged</param>
        /// <param name="basePath">The path that will be used to find the bins in the cue file if the bin paths are relative</param>
        /// <returns>The merged <see cref="CueFile"/></returns>
        public static CueFile MergeBins(Stream outputStream, string binFileName, CueFile unmergedCue)
        {
            var mergedCueFileEntry = new CueFileEntry()
            {
                FileName = binFileName,
                FileType = "BINARY",
                Tracks = new List<CueTrack>()
            };

            var cueFile = new CueFile();

            cueFile.FileEntries.Add(mergedCueFileEntry);

            long currentFrame = 0;

            var basePath = Path.GetDirectoryName(unmergedCue.Path);

            // Copy each source bin file into the merged bin
            foreach (var cueFileEntry in unmergedCue.FileEntries)
            {
                var binPath = cueFileEntry.FileName;
                var binDirectory = Path.GetDirectoryName(binPath);

                // If the bin path is relative, combine it with the base path
                if (binDirectory == "" || binDirectory.StartsWith("..") || binDirectory.StartsWith("."))
                {
                    binPath = Path.Combine(basePath, cueFileEntry.FileName);
                }

                using (var srcStream = new FileStream(binPath, FileMode.Open, FileAccess.Read))
                {
                    srcStream.CopyTo(outputStream);

                    // Copy track data to the merged bin, 
                    // and adjust the cue sheet indexes accordingly
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
                        mergedCueFileEntry.Tracks.Add(newTrack);
                    }

                    var frames = srcStream.Length / 2352;
                    currentFrame += frames;
                }

            }

            return cueFile;
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

            using (Stream stream = File.OpenRead(file))
            using (var archive = ArchiveFactory.Open(stream))
            {
                var fileNames = archive.Entries.Select(x => x.Key).ToList();
                files = fileNames.Select(x => Path.Combine(tempPath, x)).ToList();

                // https://github.com/RupertAvery/PSXPackager/pull/40
                archive.EntryExtractionBegin += (sender, args) =>
                {
                    _notifier.Notify(PopstationEventEnum.DecompressStart, args.Item.Key);
                    _currentFileDecompressedSize = args.Item.Size;
                };

                archive.EntryExtractionEnd += (sender, args) =>
                {
                    _notifier.Notify(PopstationEventEnum.DecompressProgress, 100);
                    _notifier.Notify(PopstationEventEnum.DecompressComplete, null);
                };

                archive.CompressedBytesRead += ArchiveFileOnExtracting;
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                {
                    entry.WriteToDirectory(tempPath, new ExtractionOptions()
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
                }
                archive.CompressedBytesRead -= ArchiveFileOnExtracting;

            }

            tempFiles.AddRange(files);
        }


        private void ArchiveFileOnExtracting(object sender, CompressedBytesReadEventArgs e)
        {
            var percentage = ((double)e.CompressedBytesRead / (double)_currentFileDecompressedSize) * 100;
            _notifier.Notify(PopstationEventEnum.DecompressProgress, (int)percentage);
        }


        static string GetPBPGameId(string srcPbp)
        {
            using (var stream = new FileStream(srcPbp, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpReader(stream);
                return (string)pbpStreamReader.SFOData.Entries.First(e => e.Key == SFOKeys.DISC_ID).Value;
            }
        }


        private GameEntry GetDummyGame(string gameId, string gameTitle)
        {
            return new GameEntry()
            {
                SerialID = gameId,
                GameID = gameId,
                Title = gameTitle,
                MainGameTitle = gameTitle,
                Region = "NTSC",
                MainGameID = gameId.Replace("-", "")
            };
        }

        private GameEntry GetGameEntry(string gameId, string path, bool showMessages = true)
        {
            GameEntry game;
            var dummyTitle = Path.GetFileNameWithoutExtension(path);

            if (gameId != null)
            {
                game = _gameDb.GetEntryByGameID(gameId);
                if (game == null)
                {
                    if (showMessages)
                        _notifier?.Notify(PopstationEventEnum.Warning, $"Did not find a Game with ID {gameId} Using title {dummyTitle}");

                    game = GetDummyGame(gameId, dummyTitle);
                }

                if (showMessages)
                    _notifier?.Notify(PopstationEventEnum.Info, $"Found {gameId} \"{game.Title}\"");
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
            string originalFile,
            string[] srcIsos,
            string[] srcTocs,
            ProcessOptions processOptions,
            CancellationToken cancellationToken)
        {
            var appPath = ApplicationInfo.AppPath;
            var srcIso = srcIsos[0];
            var gameId = GameDB.FindGameId(srcIso);
            var game = GetGameEntry(gameId, srcIso, false);

            var options = new ConvertOptions()
            {
                OriginalPath = Path.GetDirectoryName(originalFile),
                OriginalFilename = Path.GetFileNameWithoutExtension(originalFile),
                OutputPath = processOptions.OutputPath,
                DiscInfos = new List<DiscInfo>(),
                MainGameTitle = game.MainGameTitle,
                MainGameID = game.MainGameID,
                MainGameRegion = game.Region,
                BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                CompressionLevel = processOptions.CompressionLevel,
                CheckIfFileExists = processOptions.CheckIfFileExists,
                SkipIfFileExists = processOptions.SkipIfFileExists,
                FileNameFormat = processOptions.FileNameFormat,
            };

            if (processOptions.GenerateResourceFolders)
            {
                GenerateResourceFolders(processOptions, options, game);
                return true;
            }

            SetResources(processOptions, options, game);

            for (var i = 0; i < srcIsos.Length; i++)
            {
                gameId = GameDB.FindGameId(srcIsos[i]);
                game = GetGameEntry(gameId, srcIsos[i]);

                options.DiscInfos.Add(new DiscInfo()
                {
                    GameID = game.GameID,
                    GameTitle = game.Title,
                    SourceIso = srcIsos[i],
                    SourceToc = i < srcTocs.Length ? srcTocs[i] : "",
                });
            }

            _notifier?.Notify(PopstationEventEnum.Info, $"Using Title '{game.MainGameTitle}'");

            var popstation = new Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            return popstation.Convert(options, cancellationToken);
        }


        private void SetResources(ProcessOptions processOptions, ConvertOptions options, GameEntry entry)
        {
            var appPath = ApplicationInfo.AppPath;
            var defaultPath = Path.Combine(appPath, "Resources");


            if (string.IsNullOrEmpty(processOptions.ResourceRoot))
            {
                processOptions.ResourceRoot = options.OriginalPath;
            }

            if (string.IsNullOrEmpty(processOptions.ResourceFormat))
            {
                processOptions.ResourceFormat = "%FILENAME%\\%RESOURCE%.%EXT%";
            }


            Resource GetResourceOrDefault(ResourceType type)
            {
                string ext = Popstation.GetExtensionFromType(type);

                var filename = Popstation.GetResourceFilename(processOptions.ResourceFormat, options.OriginalFilename, entry.SerialID, entry.MainGameID, entry.Title, entry.MainGameTitle, entry.Region, type, ext);

                var resourcePath = Path.Combine(processOptions.ResourceRoot, filename);

                if (!File.Exists(resourcePath) || !processOptions.ImportResources)
                {
                    resourcePath = Path.Combine(defaultPath, $"{type}.{ext}");
                }

                var fileName = GetActualFileName(resourcePath);

                var info = new FileInfo(fileName);

                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new byte[info.Length];
                    stream.Read(buffer, 0, (int)info.Length);
                    return new Resource(type, buffer, (uint)info.Length);
                }
            }

            options.Icon0 = GetResourceOrDefault(ResourceType.ICON0);
            options.Icon1 = GetResourceOrDefault(ResourceType.ICON1);
            options.Pic0 = GetResourceOrDefault(ResourceType.PIC0);
            options.Pic1 = GetResourceOrDefault(ResourceType.PIC1);
            options.Snd0 = GetResourceOrDefault(ResourceType.SND0);
            options.Boot = GetResourceOrDefault(ResourceType.BOOT);
        }

        /// <summary>
        /// For case-sensitve file systems, this will return the actual filename of a path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        private static string GetActualFileName(string path)
        {
            var directory = Path.GetDirectoryName(path);
            var pattern = Path.GetFileName(path);
            string resultFileName;

            // Enumerate all files in the directory, using the file name as a pattern
            // This will list all case variants of the filename even on file systems that
            // are case sensitive
            var foundFiles = Directory.EnumerateFiles(directory, pattern, new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive }).ToList();

            if (foundFiles.Any())
            {
                if (foundFiles.Count > 1)
                {
                    // More than two files with the same name but different case spelling found
                    throw new Exception("Ambiguous File reference for " + path);
                }
                else
                {
                    resultFileName = foundFiles.First();
                }
            }
            else
            {
                resultFileName = path;
            }

            return resultFileName;
        }

        private void GenerateResourceFolders(ProcessOptions processOptions, ConvertOptions options, GameEntry entry)
        {
            var path = Popstation.GetResourceFilename(processOptions.ResourceFormat, options.OriginalFilename, entry.SerialID, entry.MainGameID, entry.Title, entry.MainGameTitle, entry.Region, ResourceType.ICON0, "png");

            path = Path.GetDirectoryName(path);

            path = Path.Combine(options.OriginalPath, path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            using (File.Create(Path.Combine(path, entry.Title)))
            {
            }
        }

        private bool ConvertIso(
            string originalFile,
            string srcIso,
            string srcToc,
            ProcessOptions processOptions,
            CancellationToken cancellationToken)
        {
            var appPath = ApplicationInfo.AppPath;
            var gameId = GameDB.FindGameId(srcIso);
            var game = GetGameEntry(gameId, srcIso, false);

            var options = new ConvertOptions()
            {
                DiscInfos = new List<DiscInfo>()
                {
                    new DiscInfo()
                    {
                        GameID = game.GameID,
                        GameTitle = game.MainGameTitle,
                        SourceIso = srcIso,
                        SourceToc = srcToc,
                    }
                },
                OutputPath = processOptions.OutputPath,
                OriginalPath = Path.GetDirectoryName(originalFile),
                OriginalFilename = Path.GetFileNameWithoutExtension(originalFile),
                MainGameID = game.MainGameID,
                MainGameTitle = game.MainGameTitle,
                MainGameRegion = game.Region,
                BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                CompressionLevel = processOptions.CompressionLevel,
                CheckIfFileExists = processOptions.CheckIfFileExists,
                SkipIfFileExists = processOptions.SkipIfFileExists,
                FileNameFormat = processOptions.FileNameFormat,
            };

            if (processOptions.GenerateResourceFolders)
            {
                GenerateResourceFolders(processOptions, options, game);
                return true;
            }

            SetResources(processOptions, options, game);


            _notifier.Notify(PopstationEventEnum.Info, $"Using Title '{game.Title}'");

            var popstation = new Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            return popstation.Convert(options, cancellationToken);
        }

        // TODO
        public bool RepackPBP(
            string originalFile,
            string srcPbp,
            ProcessOptions processOptions,
            CancellationToken cancellationToken)
        {
            var appPath = ApplicationInfo.AppPath;
            var gameId = GetPBPGameId(srcPbp);
            var game = GetGameEntry(gameId, srcPbp, false);


            var options = new ConvertOptions()
            {
                DiscInfos = new List<DiscInfo>()
                {
                    new DiscInfo()
                    {
                        GameID = game.GameID,
                        GameTitle = game.MainGameTitle,
                        SourceIso = srcPbp,
                    }
                },
                OriginalFilename = Path.GetFileNameWithoutExtension(originalFile),
                OriginalPath = Path.GetDirectoryName(originalFile),
                OutputPath = processOptions.OutputPath,
                MainGameTitle = game.Title,
                MainGameRegion = game.Region,
                MainGameID = game.MainGameID,
                BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                CompressionLevel = processOptions.CompressionLevel,
                CheckIfFileExists = processOptions.CheckIfFileExists,
                SkipIfFileExists = processOptions.SkipIfFileExists,
                FileNameFormat = processOptions.FileNameFormat,
            };

            if (processOptions.GenerateResourceFolders)
            {
                GenerateResourceFolders(processOptions, options, game);
                return true;
            }

            SetResources(processOptions, options, game);

            _notifier.Notify(PopstationEventEnum.Info, $"Using Title '{game.Title}'");

            var popstation = new Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            return popstation.Repack(options, cancellationToken);
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
                ResourceFormat = processOptions.ResourceFormat,
                ResourceFoldersPath = processOptions.ResourceRoot,
                FindGame = (gameId) => GetGameEntry(gameId, srcPbp, false)
            };

            var popstation = new Popstation
            {
                ActionIfFileExists = _eventHandler.ActionIfFileExists,
                Notify = _notifier.Notify,
                TempFiles = tempFiles
            };

            popstation.Extract(info, cancellationToken);
        }
    }
}
