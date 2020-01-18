using CommandLine;
using DiscUtils.Iso9660;
using Popstation;
using SevenZipExtractor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PSXPackager
{

    public class MergedBin
    {
        public string Path { get; set; }
        public List<CueFile> CueFiles { get; set; }
    }

    class Program
    {

        static bool IsCue(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".cue";
        }

        static bool IsPbp(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".pbp";
        }

        static bool IsArchive(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".7z" ||
            Path.GetExtension(filename).ToLower() == ".rar" ||
            Path.GetExtension(filename).ToLower() == ".zip";
        }

        static bool IsBin(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".bin";
        }

        static bool IsImageFile(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".bin" ||
            Path.GetExtension(filename).ToLower() == ".img" ||
            Path.GetExtension(filename).ToLower() == ".iso";
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
                    if (IsImageFile(entry.FileName) || IsCue(entry.FileName))
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

        static Task ConvertIso(string srcIso, string srcToc, string outpath, int compressionLevel, CancellationToken cancellationToken)
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:\\\\\\", "").Replace("file:///", "");
            var appPath = System.IO.Path.GetDirectoryName(path);


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
                    var gameDB = new GameDB(Path.Combine(appPath, "Resources", "gameinfo.db"));

                    game = gameDB.GetEntryByScannerID(gameId);

                    if (game != null)
                    {
                        Console.WriteLine($"Found {game.GameName}!");
                    }
                    else
                    {
                        Console.WriteLine($"Could not find gameId {gameId}!");
                    }
                }
                else
                {
                    Console.WriteLine($"Could not find SYSTEM.CNF!");
                }

            }

            if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);

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
                DestinationIso = Path.Combine(outpath, filename)
            };

            var popstation = new Popstation.Popstation();
            popstation.OnEvent = Notify;

            total = 0;

            return popstation.Extract(info, cancellationToken);
        }

        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('l', "level", Required = false, HelpText = "Set compression level 0-9, default 5", Default = 5)]
            public int CompressionLevel { get; set; }

            [Option('o', "output", Required = false
                , HelpText = "The output path where the converted file will be written")]
            public string OutputPath { get; set; }

            [Option('i', "input", Group = "input", HelpText = "The input file to convert")]
            public string InputPath { get; set; }

            [Option('b', "batch", Group = "input", HelpText = "The path to batch process a set of files")]
            public string Batch { get; set; }

            [Option('e', "ext", Required = false, HelpText = "The extension of the files to process in the batch folder, e.g. .7z")]
            public string BatchExtension { get; set; }

        }

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

        static void WriteCue(string path, IEnumerable<CueFile> cueFiles)
        {
            using (var file = new FileStream(path, FileMode.Create))
            {
                var writer = new StreamWriter(file);
                foreach (var cueFile in cueFiles)
                {
                    writer.WriteLine($"FILE \"{cueFile.FileName}\" {cueFile.FileType}");
                    foreach (var cueTrack in cueFile.Tracks)
                    {
                        writer.WriteLine($"  TRACK {cueTrack.Number:00} {cueTrack.DataType}");

                        foreach (var cueIndex in cueTrack.Indexes)
                        {
                            writer.WriteLine($"    INDEX {cueIndex.Number:00} {cueIndex.Position.ToString()}");

                        }
                    }
                }
                writer.Flush();
                writer.Close();
            }
        }

        static CancellationTokenSource cancelToken;

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

                if (IsArchive(file))
                {
                    tempFiles = Unpack(file, tempPath, cancellationToken);

                    if (cancellationToken.IsCancellationRequested) return;

                    file = "";

                    if (tempFiles.Count(IsImageFile) == 0)
                    {
                        Console.WriteLine("No image files found!");
                    }
                    else if (tempFiles.Count(IsImageFile) == 1)
                    {
                        file = tempFiles.FirstOrDefault(IsImageFile);
                    }
                    else if (tempFiles.Count(IsBin) > 1)
                    {
                        Console.WriteLine($"Multi-bin image was found!");

                        var cue = tempFiles.FirstOrDefault(IsCue);
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

                    if (IsPbp(file))
                    {
                        await ExtractPbp(file, outPath, cancellationToken);
                    }
                    else
                    {
                        if (IsCue(file))
                        {
                            var filePath = Path.GetDirectoryName(file);

                            var cueReader = new CueReader();
                            var cueFiles = cueReader.Read(file);
                            if (cueFiles.Count > 1)
                            {
                                var mergedBin = MergeBins(file, cueFiles, tempPath);
                                var cueFile = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(mergedBin.Path) + ".cue");
                                WriteCue(cueFile, mergedBin.CueFiles);
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
