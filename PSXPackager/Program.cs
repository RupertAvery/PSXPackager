using CommandLine;
using DiscUtils.Iso9660;
using Popstation;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace PSXPackager
{
    class Program
    {
        static bool IsValidArchiveFile(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".7z" ||
            Path.GetExtension(filename).ToLower() == ".rar" ||
            Path.GetExtension(filename).ToLower() == ".zip";
        }

        static bool IsValidImageFile(Entry entry)
        {
            var filename = entry.FileName;
            return Path.GetExtension(filename).ToLower() == ".bin" ||
            Path.GetExtension(filename).ToLower() == ".img" ||
            Path.GetExtension(filename).ToLower() == ".iso";
        }

        static string Unzip(string file, string tempPath)
        {
            var path = "";

            using (ArchiveFile archiveFile = new ArchiveFile(file))
            {
                if (archiveFile.Entries.Count(IsValidImageFile) == 0)
                {
                    Console.WriteLine("No valid image files found");
                }
                else if (archiveFile.Entries.Count(IsValidImageFile) == 1)
                {
                    foreach (Entry entry in archiveFile.Entries)
                    {
                        if (IsValidImageFile(entry))
                        {
                            Console.WriteLine($"Decompressing {entry.FileName}...");
                            path = Path.Combine(tempPath, entry.FileName);
                            // extract to file
                            entry.Extract(path, false);
                            break;
                        }
                    }
                }
                else if (archiveFile.Entries.Count(entry => Path.GetExtension(entry.FileName).ToLower() == ".bin") > 1)
                {
                    Console.WriteLine($"Multi-bin image was found!");

                    var files = new List<string>();
                    try
                    {
                        foreach (Entry entry in archiveFile.Entries)
                        {
                            Console.WriteLine($"Decompressing {entry.FileName}...");
                            path = Path.Combine(tempPath, entry.FileName);
                            // extract to file
                            entry.Extract(path, false);
                            files.Add(path);
                        }

                        var cue = files.FirstOrDefault(x => Path.GetExtension(x).ToLower() == ".cue");
                        if (cue != null)
                        {
                            var cueReader = new CueReader();
                            var cueFiles = cueReader.Read(cue);

                            Console.WriteLine($"Merging .bins...");

                            path = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(file) + " - JOINED.bin");
                            using (var joinedFile = new FileStream(path, FileMode.Create))
                            {
                                foreach (var cueFile in cueFiles)
                                {
                                    using (var srcStream = new FileStream(Path.Combine(tempPath, cueFile.FileName), FileMode.Open))
                                    {
                                        srcStream.CopyTo(joinedFile);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No cue sheet found!");
                        }
                    }
                    finally
                    {
                        foreach (var tempFile in files)
                        {
                            File.Delete(tempFile);
                        }
                    }

                }
            }
            return path;
        }

        static void ConvertIso(string srcIso, string outpath, int compressionLevel)
        {
            string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:\\\\\\", "").Replace("file:///", "");
            var appPath = System.IO.Path.GetDirectoryName(path);


            var regex = new Regex("(S[LC]\\w{2})_(\\d{3})\\.(\\d{2})");

            GameEntry game = null;

            using (var stream = new FileStream(srcIso, FileMode.Open))
            {
                var cdReader = new CDReader(stream, false, 2352);

                string gameId = "";

                foreach (var file in cdReader.GetFiles("\\"))
                {
                    var filename = file.Substring(1, file.LastIndexOf(";"));
                    var match = regex.Match(filename);
                    if (match.Success)
                    {
                        gameId = $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}";
                        break;
                    }
                }

                var gameDB = new GameDB(Path.Combine(appPath, "Resources", "gameinfo.db"));

                game = gameDB.GetEntryByScannerID(gameId);

                if (game != null)
                {
                    Console.WriteLine($"Found {game.GameName}!");
                }
            }

            var info = new ConvertIsoInfo()
            {
                DiscInfos = new List<DiscInfo>()
                {
                    new DiscInfo()
                    {
                        GameID = game.ScannerID,
                        GameTitle = game.GameName,
                        SourceIso = srcIso,
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

            var cancelToken = new CancellationTokenSource();
            total = 0;
            popstation.Convert(info, cancelToken.Token).GetAwaiter().GetResult();
        }

        static void ExtractPbp(string srcPbp, string outpath)
        {
            var filename = Path.GetFileNameWithoutExtension(srcPbp) + ".bin";
            var info = new ExtractIsoInfo()
            {
                SourcePbp = srcPbp,
                DestinationIso = Path.Combine(outpath, filename)
            };

            var popstation = new Popstation.Popstation();
            popstation.OnEvent = Notify;

            var cancelToken = new CancellationTokenSource();
            total = 0;
            popstation.Extract(info, cancelToken.Token).GetAwaiter().GetResult();
        }

        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('l', "level", Required = false, HelpText = "Set compression level 0-9, default 5", Default = 5)]
            public int CompressionLevel { get; set; }

            [Option('o', "output", Required = true
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

            Parser.Default.ParseArguments<Options>(args)
                 .WithParsed<Options>(o =>
                 {
                     var outPath = o.OutputPath;


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

                     Console.WriteLine($"Output: {o.OutputPath}");
                     Console.WriteLine($"Compression Level: {o.CompressionLevel}");
                     Console.WriteLine();

                     if (!string.IsNullOrEmpty(o.InputPath))
                     {
                         ProcessFile(o.InputPath, outPath, tempPath, o.CompressionLevel);
                     }
                     else if (!string.IsNullOrEmpty(o.Batch))
                     {
                         var files = Directory.GetFiles(o.Batch, $"*{o.BatchExtension}");

                         foreach (var file in files)
                         {
                             ProcessFile(file, outPath, tempPath, o.CompressionLevel);
                         }
                     }
                 });
        }

        static void ProcessFile(string file, string outPath, string tempPath, int compressionLevel)
        {
            Console.WriteLine($"Converting {file}...");

            var binPath = file;
            var isTempFile = false;

            if (IsValidArchiveFile(file))
            {
                binPath = Unzip(file, tempPath);
                isTempFile = true;
            }

            if (!string.IsNullOrEmpty(binPath))
            {
                try
                {
                    if (Path.GetExtension(binPath).ToLower() == ".pbp")
                    {
                        ExtractPbp(binPath, outPath);
                    }
                    else
                    {
                        ConvertIso(binPath, outPath, compressionLevel);
                    }
                }
                finally
                {
                    if (isTempFile)
                    {
                        File.Delete(binPath);
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
                    break;
                case PopstationEventEnum.ConvertComplete:
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
                    break;
                case PopstationEventEnum.ExtractComplete:
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
