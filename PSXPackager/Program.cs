using CommandLine;
using DiscUtils.Iso9660;
using Popstation;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PSXPackager
{
    class Program
    {
        static CancellationTokenSource cancelToken;

        static void Main(string[] args)
        {

            var tempPath = Path.Combine(Path.GetTempPath(), "PSXPackager");

            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            cancelToken = new CancellationTokenSource();

            Parser.Default.ParseArguments<Options>(args)
                 .WithParsed<Options>(async o =>
                 {

                     if (!string.IsNullOrEmpty(o.InputPath))
                     {
                         if (o.CompressionLevel < 0 || o.CompressionLevel > 9)
                         {
                             Console.WriteLine($"Invalid compression level, please enter a value from 0 to 9");
                             return;
                         }
                         Console.WriteLine($"Input: {o.InputPath}");
                     }
                     else if (!string.IsNullOrEmpty(o.Batch))
                     {
                         Console.WriteLine($"Batch: {o.Batch}");
                         Console.WriteLine($"Extension: {o.BatchExtension}");
                     }

                     if (string.IsNullOrEmpty(o.OutputPath))
                     {
                         if (!string.IsNullOrEmpty(o.InputPath))
                         {
                             o.OutputPath = Path.GetDirectoryName(o.InputPath);
                         }
                         else if (!string.IsNullOrEmpty(o.Batch))
                         {
                             o.OutputPath = o.Batch;
                         }
                     }

                     Console.WriteLine($"Output: {o.OutputPath}");
                     Console.WriteLine($"Compression Level: {o.CompressionLevel}");
                     Console.WriteLine();

                     if (!string.IsNullOrEmpty(o.InputPath))
                     {
                         ProcessFile(o.InputPath, o.OutputPath, tempPath, o.CompressionLevel, cancelToken.Token).GetAwaiter().GetResult();
                     }
                     else if (!string.IsNullOrEmpty(o.Batch))
                     {
                         var files = Directory.GetFiles(o.Batch, $"*{o.BatchExtension}");

                         foreach (var file in files)
                         {
                             ProcessFile(file, o.OutputPath, tempPath, o.CompressionLevel, cancelToken.Token).GetAwaiter().GetResult();
                             if (cancelToken.Token.IsCancellationRequested)
                             {
                                 break;
                             }
                         }
                     }
                 });
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            if (!cancelToken.IsCancellationRequested)
            {
                Console.WriteLine("Stopping conversion...");
                cancelToken.Cancel();
            }
            args.Cancel = true;
        }

        static async Task ProcessFile(string file, string outPath, string tempPath, int compressionLevel, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Converting {file}...");

            List<string> tempFiles = null;
            string srcToc = null;

            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);
            try
            {

                if (FileExtensionHelper.IsArchive(file))
                {
                    tempFiles = Unpack(file, tempPath, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    file = "";

                    if (tempFiles.Count(FileExtensionHelper.IsImageFile) == 0)
                    {
                        Console.WriteLine("No image files found!");
                    }
                    else if (tempFiles.Count(FileExtensionHelper.IsImageFile) == 1)
                    {
                        file = tempFiles.FirstOrDefault(FileExtensionHelper.IsImageFile);
                    }
                    else if (tempFiles.Count(FileExtensionHelper.IsBin) > 1)
                    {
                        Console.WriteLine($"Multi-bin image was found!");

                        var cue = tempFiles.FirstOrDefault(FileExtensionHelper.IsCue);
                        if (cue != null)
                        {
                            file = cue;
                        }
                        else
                        {
                            Console.WriteLine($"No cue sheet found!");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(file))
                {

                    if (FileExtensionHelper.IsPbp(file))
                    {
                        await ExtractPbp(file, outPath, cancellationToken);
                    }
                    else
                    {
                        if (FileExtensionHelper.IsCue(file))
                        {
                            var filePath = Path.GetDirectoryName(file);

                            var cueFiles = CueReader.Read(file);
                            if (cueFiles.Count > 1)
                            {
                                var mergedBin = MergeBins(file, cueFiles, tempPath);
                                var cueFile = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(mergedBin.Path) + ".cue");
                                CueWriter.Write(cueFile, mergedBin.CueFiles);
                                srcToc = cueFile;
                                file = mergedBin.Path;

                                tempFiles.Add(mergedBin.Path);
                                tempFiles.Add(cueFile);
                            }
                            else
                            {
                                srcToc = file;
                                file = Path.Combine(filePath, cueFiles.First().FileName);
                            }
                        }

                        await ConvertIso(file, srcToc, outPath, compressionLevel, cancellationToken);
                    }


                }
            }
            finally
            {
                Console.CursorVisible = true;
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Conversion cancelled");
                }
                else
                {
                    Console.WriteLine("Conversion completed!");
                }

                if (tempFiles != null)
                {
                    foreach (var tempFile in tempFiles)
                    {
                        File.Delete(tempFile);
                    }
                }
            }
        }


        static MergedBin MergeBins(string file, IEnumerable<CueFile> cueFiles, string tempPath)
        {
            var mergedBin = new MergedBin();
            mergedBin.CueFiles = new List<CueFile>();

            Console.WriteLine($"Merging .bins...");
            long currentFrame = 0;

            var mergedFilename = Path.GetFileNameWithoutExtension(file) + " - MERGED.bin";

            mergedBin.Path = Path.Combine(tempPath, mergedFilename);

            var mcueFile = new CueFile()
            {
                FileName = mergedFilename,
                FileType = "BINARY",
                Tracks = new List<CueTrack>()
            };

            mergedBin.CueFiles.Add(mcueFile);

            using (var joinedFile = new FileStream(mergedBin.Path, FileMode.Create))
            {
                foreach (var cueFile in cueFiles)
                {
                    using (var srcStream = new FileStream(Path.Combine(tempPath, cueFile.FileName), FileMode.Open))
                    {
                        srcStream.CopyTo(joinedFile);

                        foreach (var item in cueFile.Tracks)
                        {
                            var indexes = new List<CueIndex>();
                            foreach (var idx in item.Indexes)
                            {
                                var newIndex = new CueIndex();
                                newIndex.Number = idx.Number;
                                newIndex.Position = idx.Position + Helper.PositionFromFrames(currentFrame);
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

        static List<string> Unpack(string file, string tempPath, CancellationToken cancellationToken)
        {
            var files = new List<string>();

            using (ArchiveFile archiveFile = new ArchiveFile(file))
            {
                var unpackTasks = new List<Task>();
                foreach (Entry entry in archiveFile.Entries)
                {
                    if (FileExtensionHelper.IsImageFile(entry.FileName) || FileExtensionHelper.IsCue(entry.FileName))
                    {
                        Console.WriteLine($"Decompressing {entry.FileName}...");
                        var path = Path.Combine(tempPath, entry.FileName);
                        // extract to file
                        entry.Extract(path, false);
                        files.Add(path);
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return files;
                    }
                }


            }

            return files;
        }

        static GameEntry FindGameInfo(string srcIso)
        {
            var regex = new Regex("(S[LC]\\w{2})[_-](\\d{3})\\.(\\d{2})");
            var bootRegex = new Regex("BOOT\\s*=\\s*cdrom:\\\\?(?:.*?\\\\)?(S[LC]\\w{2}[_-]?\\d{3}\\.\\d{2});1");

            GameEntry game = null;

            using (var stream = new FileStream(srcIso, FileMode.Open))
            {
                var cdReader = new CDReader(stream, false, 2352);

                string gameId = "";

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
                    if (filename == "SYSTEM.CNF")
                    {
                        syscnfFound = true;
                        using (var datastream = cdReader.OpenFile(file, FileMode.Open))
                        {
                            datastream.Seek(24, SeekOrigin.Begin);
                            var textReader = new StreamReader(datastream);
                            var bootLine = textReader.ReadLine();
                            var bootmatch = bootRegex.Match(bootLine);
                            if (bootmatch.Success)
                            {
                                var match = regex.Match(bootmatch.Groups[1].Value);
                                if (match.Success)
                                {
                                    gameId = $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}";
                                    break;
                                }
                            }
                        }
                    }
                }

                if (syscnfFound)
                {
                    var gameDB = new GameDB(Path.Combine(ApplicationInfo.AppPath, "Resources", "gameinfo.db"));

                    game = gameDB.GetEntryByScannerID(gameId);

                    if (game != null)
                    {
                        Console.WriteLine($"Found {game.GameName}!");
                    }
                    else
                    {
                        Console.WriteLine($"Could not find gameId {gameId}!");
                        return null;
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find SYSTEM.CNF!");
                    return null;
                }

            }

            return game;
        }

        static Task ConvertIso(string srcIso, string srcToc, string outpath, int compressionLevel, CancellationToken cancellationToken)
        {
            var game = FindGameInfo(srcIso);
            var appPath = ApplicationInfo.AppPath;

            var info = new ConvertIsoInfo()
            {
                DiscInfos = new List<DiscInfo>()
                {
                    new DiscInfo()
                    {
                        GameID = game.ScannerID,
                        GameTitle = game.GameName,
                        SourceIso = srcIso,
                        SourceToc = srcToc,
                    }
                },
                DestinationPbp = Path.Combine(outpath, $"{game.GameName}.PBP"),
                MainGameTitle = game.GameName,
                MainGameID = game.SaveFolderName,
                SaveTitle = game.SaveDescription,
                SaveID = game.SaveFolderName,
                Pic0 = Path.Combine(appPath, "Resources", "PIC0.PNG"),
                Pic1 = Path.Combine(appPath, "Resources", "PIC1.PNG"),
                Icon0 = Path.Combine(appPath, "Resources", "ICON0.PNG"),
                BasePbp = Path.Combine(appPath, "Resources", "BASE.PBP"),
                CompressionLevel = compressionLevel
            };

            var popstation = new Popstation.Popstation();
            popstation.OnEvent = Notify;

            total = 0;

            return popstation.Convert(info, cancellationToken);
        }

        static Task ExtractPbp(string srcPbp, string outpath, CancellationToken cancellationToken)
        {
            var filename = Path.GetFileNameWithoutExtension(srcPbp) + ".bin";

            var info = new ExtractIsoInfo()
            {
                SourcePbp = srcPbp,
                DestinationIso = Path.Combine(outpath, filename),
                CreateCuesheet = true
            };

            var popstation = new Popstation.Popstation();
            popstation.OnEvent = Notify;

            total = 0;

            return popstation.Extract(info, cancellationToken);
        }

        static int y;
        static long total;
        static long lastTicks;

        private static void Notify(PopstationEventEnum @event, object value)
        {
            switch (@event)
            {
                case PopstationEventEnum.GetIsoSize:
                    total = Convert.ToInt64(value);
                    break;
                case PopstationEventEnum.ConvertSize:
                    total = Convert.ToInt64(value);
                    break;
                case PopstationEventEnum.ConvertStart:
                    y = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;
                case PopstationEventEnum.ConvertComplete:
                    Console.CursorVisible = true;
                    Console.WriteLine();
                    break;
                case PopstationEventEnum.ConvertProgress:
                    Console.SetCursorPosition(0, y);
                    if (DateTime.Now.Ticks - lastTicks > 100000)
                    {
                        Console.Write($"Converting: {Math.Round(Convert.ToInt32(value) / (double)total * 100, 0) }%");
                        lastTicks = DateTime.Now.Ticks;
                    }
                    break;
                case PopstationEventEnum.ExtractStart:
                    y = Console.CursorTop;
                    Console.CursorVisible = false;
                    break;
                case PopstationEventEnum.ExtractComplete:
                    Console.CursorVisible = true;
                    Console.WriteLine();
                    break;
                case PopstationEventEnum.ExtractProgress:
                    Console.SetCursorPosition(0, y);
                    if (DateTime.Now.Ticks - lastTicks > 100000)
                    {
                        Console.Write($"Converting: {Math.Round(Convert.ToInt32(value) / (double)total * 100, 0) }%");
                        lastTicks = DateTime.Now.Ticks;
                    }
                    break;
            }
        }

    }
}
